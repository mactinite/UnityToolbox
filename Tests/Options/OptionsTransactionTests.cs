using System;
using System.Linq;
using NUnit.Framework;
using toolbox.Options;

namespace toolbox.Tests
{
    public class OptionsTransactionTests
    {
        InMemoryOptionsStorage storage;
        OptionsStore store;
        ChoiceOption mode;
        ChoiceOption resolution;
        BoolOption vsync;

        [SetUp]
        public void SetUp()
        {
            storage = new InMemoryOptionsStorage();
            store = new OptionsStore(storage);
            mode = new ChoiceOption("display.mode", "Borderless", "Borderless", "Windowed") { Flags = OptionFlags.Confirm };
            resolution = new ChoiceOption("display.resolution", "1920x1080", "1920x1080", "1280x720") { Flags = OptionFlags.Confirm };
            vsync = new BoolOption("display.vsync", true);
            store.Register(mode, resolution, vsync);
            store.Load();
        }

        [TearDown]
        public void TearDown() => store.Dispose();

        [Test]
        public void ForFlagged_TracksConfirmOptionsOnly()
        {
            var transaction = OptionsTransaction.ForFlagged(store);

            Assert.That(transaction.Options, Is.EquivalentTo(new Option[] { mode, resolution }));
            Assert.That(transaction.IsTracking(vsync), Is.False);
        }

        [Test]
        public void Set_StagesWithoutTouchingTheOption()
        {
            var transaction = OptionsTransaction.ForFlagged(store);

            transaction.Set(mode, "Windowed");

            Assert.That(mode.Value, Is.EqualTo("Borderless"));
            Assert.That(transaction.Get(mode), Is.EqualTo("Windowed"));
            Assert.That(transaction.Get(resolution), Is.EqualTo("1920x1080"));
            Assert.That(transaction.IsPending(mode), Is.True);
            Assert.That(transaction.IsPending(resolution), Is.False);
            Assert.That(transaction.HasChanges, Is.True);
            Assert.That(transaction.IsApplied, Is.False);
        }

        [Test]
        public void Set_BackToCurrentValue_IsNotPending()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            transaction.Set(mode, "Windowed");
            transaction.Set(mode, "Borderless");

            Assert.That(transaction.HasChanges, Is.False);
            Assert.That(transaction.IsPending(mode), Is.False);
        }

        [Test]
        public void Set_UntrackedOptionThrows()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            Assert.Throws<ArgumentException>(() => transaction.Set(vsync, false));
        }

        [Test]
        public void Apply_WritesValues_AndNotifiesAppliers()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            int applied = 0;
            mode.ValueChanged += _ => applied++;
            transaction.Set(mode, "Windowed");

            transaction.Apply();

            Assert.That(mode.Value, Is.EqualTo("Windowed"));
            Assert.That(applied, Is.EqualTo(1));
            Assert.That(transaction.IsApplied, Is.True);
            Assert.That(transaction.HasChanges, Is.False);
            Assert.That(storage.Writes, Is.EqualTo(0), "nothing is saved until Commit");
        }

        [Test]
        public void Revert_RestoresBaseline_AndNotifies()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            int changes = 0;
            resolution.ValueChanged += _ => changes++;
            transaction.Set(mode, "Windowed");
            transaction.Set(resolution, "1280x720");
            transaction.Apply();

            transaction.Revert();

            Assert.That(mode.Value, Is.EqualTo("Borderless"));
            Assert.That(resolution.Value, Is.EqualTo("1920x1080"));
            Assert.That(changes, Is.EqualTo(2), "one for apply, one for revert");
            Assert.That(transaction.IsApplied, Is.False);
            Assert.That(transaction.HasChanges, Is.False);
        }

        [Test]
        public void Commit_Saves_AndMovesTheBaseline()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            transaction.Set(mode, "Windowed");
            transaction.Apply();

            transaction.Commit();

            Assert.That(storage.Writes, Is.EqualTo(1));
            Assert.That(transaction.IsApplied, Is.False);
            transaction.Revert();
            Assert.That(mode.Value, Is.EqualTo("Windowed"), "the committed value is the new baseline");
        }

        [Test]
        public void Commit_AppliesPendingFirst()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            transaction.Set(resolution, "1280x720");

            transaction.Commit();

            Assert.That(resolution.Value, Is.EqualTo("1280x720"));
            Assert.That(OptionsFile.FromJson(storage.Text).TryGet("display.resolution", out var saved) && saved == "1280x720", Is.True);
        }

        [Test]
        public void Discard_DropsPendingWithoutApplying()
        {
            var transaction = OptionsTransaction.ForFlagged(store);
            transaction.Set(mode, "Windowed");

            transaction.Discard();

            Assert.That(transaction.HasChanges, Is.False);
            Assert.That(mode.Value, Is.EqualTo("Borderless"));
        }

        [Test]
        public void Apply_CoercesThroughTheOption()
        {
            var transaction = new OptionsTransaction(store, new[] { mode });
            transaction.Set(mode, "Nonsense");

            transaction.Apply();

            Assert.That(mode.Value, Is.EqualTo("Borderless"));
            Assert.That(transaction.Options.Single(), Is.SameAs(mode));
        }
    }
}
