using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using toolbox.Options;

namespace toolbox.Tests
{
    public class OptionsStoreTests
    {
        InMemoryOptionsStorage storage;
        OptionsStore store;
        FloatOption volume;
        BoolOption shake;
        ChoiceOption mode;

        [SetUp]
        public void SetUp()
        {
            storage = new InMemoryOptionsStorage();
            store = new OptionsStore(storage);
            volume = new FloatOption("audio.master", 1f);
            shake = new BoolOption("gameplay.screenShake", true);
            mode = new ChoiceOption("display.mode", "Borderless", "Borderless", "Windowed");
            store.Register(volume, shake, mode);
        }

        [TearDown]
        public void TearDown() => store.Dispose();

        static string FileWith(params (string key, string value)[] entries)
        {
            var file = new OptionsFile();
            foreach (var (key, value) in entries)
                file.Set(key, value);
            return file.ToJson();
        }

        [Test]
        public void Register_DuplicateIdThrows_SameInstanceIgnored_NullSkipped()
        {
            Assert.Throws<ArgumentException>(() => store.Register(new BoolOption("audio.master")));
            store.Register(volume, null);
            Assert.That(store.Count, Is.EqualTo(3));
            Assert.That(volume.Store, Is.SameAs(store));
        }

        [Test]
        public void Register_OptionFromAnotherStoreThrows()
        {
            using (var other = new OptionsStore(new InMemoryOptionsStorage()))
                Assert.Throws<InvalidOperationException>(() => other.Register(volume));
        }

        [Test]
        public void Load_WithoutFile_KeepsDefaultsAndMarksLoaded()
        {
            volume.Value = 0.2f;

            store.Load();

            Assert.That(store.IsLoaded, Is.True);
            Assert.That(store.IsDirty, Is.False);
            Assert.That(volume.Value, Is.EqualTo(1f));
            Assert.That(storage.Writes, Is.EqualTo(0));
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            store.Load();
            volume.Value = 0.3f;
            shake.Value = false;
            mode.Value = "Windowed";
            Assert.That(store.IsDirty, Is.True);

            store.Save();
            Assert.That(store.IsDirty, Is.False);
            Assert.That(storage.Writes, Is.EqualTo(1));

            volume.Value = 1f;
            shake.Value = true;
            mode.Value = "Borderless";
            store.Load();

            Assert.That(volume.Value, Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(shake.Value, Is.False);
            Assert.That(mode.Value, Is.EqualTo("Windowed"));
        }

        [Test]
        public void Load_UnknownKeysSurviveSave()
        {
            storage.Text = FileWith(("future.thing", "42"), ("audio.master", "0.5"));

            store.Load();
            store.Save();

            var file = OptionsFile.FromJson(storage.Text);
            Assert.That(file.TryGet("future.thing", out var kept), Is.True);
            Assert.That(kept, Is.EqualTo("42"));
            Assert.That(file.TryGet("audio.master", out var master), Is.True);
            Assert.That(master, Is.EqualTo("0.5"));
        }

        [Test]
        public void Load_AdoptsLegacyId_AndDropsItOnSave()
        {
            var sfx = new FloatOption("audio.sfx", 1f) { LegacyIds = new[] { "audio.effects" } };
            store.Register(sfx);
            storage.Text = FileWith(("audio.effects", "0.25"));

            store.Load();
            Assert.That(sfx.Value, Is.EqualTo(0.25f));

            store.Save();
            var file = OptionsFile.FromJson(storage.Text);
            Assert.That(file.Contains("audio.effects"), Is.False);
            Assert.That(file.TryGet("audio.sfx", out var value) && value == "0.25", Is.True);
        }

        [Test]
        public void Load_MissingKeyResetsToDefault()
        {
            store.Load();
            volume.Value = 0.2f;
            store.Save();

            var file = OptionsFile.FromJson(storage.Text);
            file.Remove("audio.master");
            storage.Text = file.ToJson();
            store.Load();

            Assert.That(volume.Value, Is.EqualTo(1f));
        }

        [Test]
        public void Load_UnreadableValueFallsBackToDefault()
        {
            storage.Text = FileWith(("audio.master", "loud"), ("gameplay.screenShake", "false"));

            store.Load();

            Assert.That(volume.Value, Is.EqualTo(1f));
            Assert.That(shake.Value, Is.False);
        }

        [Test]
        public void Load_CorruptTextFallsBackToDefaults()
        {
            storage.Text = "{not json";
            volume.Value = 0.4f;

            store.Load();

            Assert.That(store.IsLoaded, Is.True);
            Assert.That(volume.Value, Is.EqualTo(1f));
        }

        [Test]
        public void Load_RunsMigrationsUpToSchemaVersion()
        {
            store.SchemaVersion = 3;
            int ran = 0;
            store.AddMigration(1, file =>
            {
                ran++;
                file.Rename("audio.volume", "audio.master");
            });
            store.AddMigration(2, file =>
            {
                ran++;
                if (file.TryGet("audio.master", out var text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
                    file.Set("audio.master", (percent / 100f).ToString(CultureInfo.InvariantCulture));
            });
            storage.Text = new OptionsFile { version = 1 }.Set("audio.volume", "50").ToJson();

            store.Load();

            Assert.That(ran, Is.EqualTo(2));
            Assert.That(volume.Value, Is.EqualTo(0.5f));
            store.Save();
            Assert.That(OptionsFile.FromJson(storage.Text).version, Is.EqualTo(3));
        }

        [Test]
        public void Load_VersionGapWithoutMigration_StillBumps()
        {
            store.SchemaVersion = 2;
            storage.Text = new OptionsFile { version = 1 }.Set("audio.master", "0.5").ToJson();

            store.Load();
            store.Save();

            Assert.That(volume.Value, Is.EqualTo(0.5f));
            Assert.That(OptionsFile.FromJson(storage.Text).version, Is.EqualTo(2));
        }

        [Test]
        public void Load_NewerFile_KeepsValuesAndVersion()
        {
            storage.Text = new OptionsFile { version = 9 }.Set("audio.master", "0.5").Set("v9.only", "x").ToJson();

            store.Load();
            store.Save();

            Assert.That(volume.Value, Is.EqualTo(0.5f));
            var file = OptionsFile.FromJson(storage.Text);
            Assert.That(file.version, Is.EqualTo(9));
            Assert.That(file.Contains("v9.only"), Is.True);
        }

        [Test]
        public void Register_AfterLoad_AdoptsStoredValue()
        {
            storage.Text = FileWith(("late.option", "true"));
            store.Load();

            var late = new BoolOption("late.option");
            store.Register(late);

            Assert.That(late.Value, Is.True);
            Assert.That(store.IsDirty, Is.False);
            store.Save();
            var file = OptionsFile.FromJson(storage.Text);
            Assert.That(file.values.Count(entry => entry.key == "late.option"), Is.EqualTo(1));
        }

        [Test]
        public void ResetAll_ByCategory_ThenEverything()
        {
            store.Load();
            volume.Value = 0.1f;
            shake.Value = false;

            store.ResetAll("Audio");
            Assert.That(volume.Value, Is.EqualTo(1f));
            Assert.That(shake.Value, Is.False);

            store.ResetAll();
            Assert.That(shake.Value, Is.True);
        }

        [Test]
        public void OptionChanged_RaisedWithOption_AndDirtyTracksChanges()
        {
            store.Load();
            Option seen = null;
            store.OptionChanged += option => seen = option;

            shake.Value = false;

            Assert.That(seen, Is.SameAs(shake));
            Assert.That(store.IsDirty, Is.True);
            store.SaveIfDirty();
            Assert.That(storage.Writes, Is.EqualTo(1));
            store.SaveIfDirty();
            Assert.That(storage.Writes, Is.EqualTo(1));
        }

        [Test]
        public void Bind_DefersUntilLoad_AndAppliesOnce()
        {
            storage.Text = FileWith(("audio.master", "0.3"));
            int calls = 0;
            float seen = -1f;

            using (volume.Bind(v => { calls++; seen = v; }))
            {
                Assert.That(calls, Is.EqualTo(0));
                store.Load();
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(seen, Is.EqualTo(0.3f).Within(1e-6f));
            }
        }

        [Test]
        public void Bind_AppliesOnce_WhenLoadLeavesDefault()
        {
            int calls = 0;
            using (volume.Bind(_ => calls++))
            {
                store.Load();
                Assert.That(calls, Is.EqualTo(1));
            }
        }

        [Test]
        public void Bind_AfterLoad_AppliesImmediately()
        {
            store.Load();
            int calls = 0;
            using (volume.Bind(_ => calls++))
                Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Categories_InRegistrationOrder_AndInCategorySortedByOrder()
        {
            store.Register(new BoolOption("audio.mute") { Order = -1 });

            Assert.That(store.Categories.ToList(), Is.EqualTo(new[] { "Audio", "Gameplay", "Display" }));
            Assert.That(store.InCategory("Audio").Select(option => option.Id).ToList(), Is.EqualTo(new[] { "audio.mute", "audio.master" }));
            Assert.That(store.Get<FloatOption>("audio.master"), Is.SameAs(volume));
            Assert.That(store.Get("nope"), Is.Null);
            Assert.That(store.TryGet("display.mode", out var found) && found == mode, Is.True);
        }

        [Test]
        public void Catalog_CollectsStaticOptionFields_InDeclarationOrder()
        {
            var collected = OptionsCatalog.Collect(typeof(TestCatalog));
            Assert.That(collected.Select(option => option.Id).ToList(), Is.EqualTo(new[] { "test.a", "test.b" }));

            using (var catalogStore = new OptionsStore(new InMemoryOptionsStorage()))
            {
                catalogStore.RegisterCatalog(typeof(TestCatalog));
                Assert.That(catalogStore.Get("test.a"), Is.SameAs(TestCatalog.A));
            }

            Assert.That(OptionsCatalog.FindCatalogTypes(), Does.Contain(typeof(TestCatalog)));
        }

        [Test]
        public void DefaultStore_IsReplaceable()
        {
            var previous = OptionsStore.HasDefault ? OptionsStore.Default : null;
            try
            {
                OptionsStore.Default = store;
                Assert.That(OptionsStore.Default, Is.SameAs(store));
            }
            finally
            {
                OptionsStore.Default = previous;
            }
        }

        [Test]
        public void Dispose_DetachesOptions()
        {
            store.Dispose();
            Assert.That(volume.Store, Is.Null);
            Assert.That(store.Count, Is.EqualTo(0));

            using (var other = new OptionsStore(new InMemoryOptionsStorage()))
            {
                other.Register(volume);
                Assert.That(volume.Store, Is.SameAs(other));
            }
        }

        [Test]
        public void DebugDump_ListsEveryOption()
        {
            volume.Value = 0.5f;
            string dump = store.DebugDump();
            Assert.That(dump, Does.Contain("audio.master = 0.5  (default 1)"));
            Assert.That(dump, Does.Contain("gameplay.screenShake = true"));
        }

        [OptionsCatalog]
        static class TestCatalog
        {
            public static readonly BoolOption A = new BoolOption("test.a");
            internal static readonly IntOption B = new IntOption("test.b", 1);
            static readonly string NotAnOption = "ignored";
        }
    }
}
