using System.Collections.Generic;
using NUnit.Framework;
using toolbox.AbilitySystem;

namespace toolbox.Tests
{
    public class CountedTagSetTests
    {
        private GameplayTag status;
        private GameplayTag stunned;
        private GameplayTag element;
        private CountedTagSet set;

        [SetUp]
        public void SetUp()
        {
            status = TestTags.Create("Status");
            stunned = TestTags.Create("Stunned", status);
            element = TestTags.Create("Element");
            set = new CountedTagSet();
        }

        [TearDown]
        public void TearDown() => TestTags.DestroyAll();

        [Test]
        public void AddTag_MakesTagOwned()
        {
            set.AddTag(element);

            Assert.That(set.HasTag(element), Is.True);
            Assert.That(set.GetExplicitCount(element), Is.EqualTo(1));
        }

        [Test]
        public void RefCounting_TagOwnedUntilLastGrantReleased()
        {
            set.AddTag(element);
            set.AddTag(element);

            set.RemoveTag(element);
            Assert.That(set.HasTag(element), Is.True);

            set.RemoveTag(element);
            Assert.That(set.HasTag(element), Is.False);
        }

        [Test]
        public void TagAdded_FiresOnlyOnFirstGrant()
        {
            var added = new List<GameplayTag>();
            set.TagAdded += added.Add;

            set.AddTag(element);
            set.AddTag(element);

            Assert.That(added, Is.EqualTo(new[] { element }));
        }

        [Test]
        public void TagRemoved_FiresOnlyOnLastRelease()
        {
            set.AddTag(element, 2);
            var removed = new List<GameplayTag>();
            set.TagRemoved += removed.Add;

            set.RemoveTag(element);
            Assert.That(removed, Is.Empty);

            set.RemoveTag(element);
            Assert.That(removed, Is.EqualTo(new[] { element }));
        }

        [Test]
        public void OwningDescendant_CountsAsOwningAncestor()
        {
            set.AddTag(stunned);

            Assert.That(set.HasTag(status), Is.True);
            Assert.That(set.GetExplicitCount(status), Is.Zero);

            set.RemoveTag(stunned);
            Assert.That(set.HasTag(status), Is.False);
        }

        [Test]
        public void AncestorEvents_FireOnDescendantTransitions()
        {
            var added = new List<GameplayTag>();
            var removed = new List<GameplayTag>();
            set.TagAdded += added.Add;
            set.TagRemoved += removed.Add;

            set.AddTag(stunned);
            Assert.That(added, Is.EquivalentTo(new[] { stunned, status }));

            set.AddTag(status);
            Assert.That(added.Count, Is.EqualTo(2), "already-owned ancestor must not re-fire");

            set.RemoveTag(stunned);
            Assert.That(removed, Is.EqualTo(new[] { stunned }), "status still owned explicitly");

            set.RemoveTag(status);
            Assert.That(removed, Is.EquivalentTo(new[] { stunned, status }));
        }

        [Test]
        public void RemoveTag_NotOwned_ReturnsFalse()
        {
            Assert.That(set.RemoveTag(element), Is.False);
        }

        [Test]
        public void RemoveTag_ClampsAtZero()
        {
            set.AddTag(element, 2);

            Assert.That(set.RemoveTag(element, 5), Is.True);
            Assert.That(set.HasTag(element), Is.False);

            set.AddTag(element);
            Assert.That(set.HasTag(element), Is.True, "over-removal must not leave a negative balance");
        }

        [Test]
        public void ContainerQueries()
        {
            set.AddTag(stunned);
            var owned = new GameplayTagContainer(status);
            var mixed = new GameplayTagContainer(status, element);
            var missing = new GameplayTagContainer(element);
            var empty = new GameplayTagContainer();

            Assert.That(set.HasAll(owned), Is.True);
            Assert.That(set.HasAll(mixed), Is.False);
            Assert.That(set.HasAny(mixed), Is.True);
            Assert.That(set.HasAny(missing), Is.False);
            Assert.That(set.HasNone(missing), Is.True);
            Assert.That(set.HasNone(mixed), Is.False);

            Assert.That(set.HasAll(empty), Is.True);
            Assert.That(set.HasAny(empty), Is.False);
            Assert.That(set.HasNone(empty), Is.True);
        }
    }
}
