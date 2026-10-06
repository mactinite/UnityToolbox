using System;
using NUnit.Framework;
using toolbox.Options;

namespace toolbox.Tests
{
    public class OptionsFileTests
    {
        [Test]
        public void Set_TryGet_Remove_Rename()
        {
            var file = new OptionsFile();

            file.Set("a", "1").Set("b", "2").Set("a", "3");

            Assert.That(file.values.Count, Is.EqualTo(2));
            Assert.That(file.TryGet("a", out var a) && a == "3", Is.True);
            Assert.That(file.Contains("b"), Is.True);
            Assert.That(file.Remove("b"), Is.True);
            Assert.That(file.Remove("b"), Is.False);
            Assert.That(file.Rename("a", "c"), Is.True);
            Assert.That(file.Contains("a"), Is.False);
            Assert.That(file.TryGet("c", out var c) && c == "3", Is.True);
            Assert.That(file.Rename("missing", "d"), Is.False);
        }

        [Test]
        public void Rename_OntoExistingKey_DropsTheOldValue()
        {
            var file = new OptionsFile().Set("old", "1").Set("new", "2");

            file.Rename("old", "new");

            Assert.That(file.TryGet("new", out var value) && value == "2", Is.True);
            Assert.That(file.Contains("old"), Is.False);
        }

        [Test]
        public void TryGet_LastDuplicateWins()
        {
            var file = new OptionsFile();
            file.values.Add(new OptionsFile.Entry("k", "first"));
            file.values.Add(new OptionsFile.Entry("k", "second"));

            Assert.That(file.TryGet("k", out var value) && value == "second", Is.True);
            Assert.That(file.ToDictionary()["k"], Is.EqualTo("second"));
        }

        [Test]
        public void Json_RoundTrips()
        {
            var file = new OptionsFile { version = 4 }.Set("audio.master", "0.5").Set("display.mode", "Windowed");

            var parsed = OptionsFile.FromJson(file.ToJson());
            var compact = OptionsFile.FromJson(file.ToJson(prettyPrint: false));

            Assert.That(parsed.version, Is.EqualTo(4));
            Assert.That(parsed.values.Count, Is.EqualTo(2));
            Assert.That(parsed.TryGet("display.mode", out var mode) && mode == "Windowed", Is.True);
            Assert.That(compact.ToDictionary(), Is.EquivalentTo(parsed.ToDictionary()));
            Assert.That(file.ToJson(), Does.Contain("\"version\": 4"));
        }

        [Test]
        public void FromJson_RejectsGarbageAndEmpty()
        {
            Assert.Catch<Exception>(() => OptionsFile.FromJson("{nope"));
            Assert.Throws<ArgumentException>(() => OptionsFile.FromJson(""));
            Assert.Throws<ArgumentException>(() => OptionsFile.FromJson(null));
        }

        [Test]
        public void FromJson_ToleratesMissingValues()
        {
            var parsed = OptionsFile.FromJson("{\"version\":2}");

            Assert.That(parsed.version, Is.EqualTo(2));
            Assert.That(parsed.values, Is.Not.Null);
            Assert.That(parsed.values.Count, Is.EqualTo(0));
        }
    }
}
