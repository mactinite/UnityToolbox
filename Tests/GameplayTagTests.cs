using NUnit.Framework;
using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Tests
{
    public class GameplayTagTests
    {
        private GameplayTag root;
        private GameplayTag child;
        private GameplayTag grandchild;
        private GameplayTag unrelated;

        [SetUp]
        public void SetUp()
        {
            root = TestTags.Create("Status");
            child = TestTags.Create("Debuff", root);
            grandchild = TestTags.Create("Stunned", child);
            unrelated = TestTags.Create("Element");
        }

        [TearDown]
        public void TearDown() => TestTags.DestroyAll();

        [Test]
        public void Matches_Self()
        {
            Assert.That(root.Matches(root), Is.True);
        }

        [Test]
        public void Descendant_Matches_Ancestor()
        {
            Assert.That(child.Matches(root), Is.True);
            Assert.That(grandchild.Matches(root), Is.True);
            Assert.That(grandchild.Matches(child), Is.True);
        }

        [Test]
        public void Ancestor_DoesNotMatch_Descendant()
        {
            Assert.That(root.Matches(child), Is.False);
            Assert.That(child.Matches(grandchild), Is.False);
        }

        [Test]
        public void Unrelated_Tags_DoNotMatch()
        {
            Assert.That(child.Matches(unrelated), Is.False);
            Assert.That(unrelated.Matches(root), Is.False);
        }

        [Test]
        public void IsDescendantOf_IsStrict()
        {
            Assert.That(root.IsDescendantOf(root), Is.False);
        }

        [Test]
        public void ParentCycle_Terminates()
        {
            var a = TestTags.Create("A");
            var b = TestTags.Create("B", a);
            a.parent = b; // authored mistake: cycle

            Assert.That(a.IsDescendantOf(unrelated), Is.False);
            Assert.That(a.GetFullPath(), Does.EndWith("A"));
        }

        [Test]
        public void GetFullPath_WalksAncestors()
        {
            Assert.That(grandchild.GetFullPath(), Is.EqualTo("Status.Debuff.Stunned"));
            Assert.That(root.GetFullPath(), Is.EqualTo("Status"));
        }
    }

    /// <summary>Builds tag assets for tests and cleans them up afterwards.</summary>
    internal static class TestTags
    {
        private static readonly System.Collections.Generic.List<GameplayTag> created = new();

        public static GameplayTag Create(string tagName, GameplayTag parent = null)
        {
            var tag = ScriptableObject.CreateInstance<GameplayTag>();
            tag.name = tagName;
            tag.parent = parent;
            created.Add(tag);
            return tag;
        }

        public static void DestroyAll()
        {
            foreach (var tag in created)
                Object.DestroyImmediate(tag);
            created.Clear();
        }
    }
}
