using System.Collections.Generic;
using NUnit.Framework;
using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Tests
{
    public class AttributeSetTests
    {
        private AttributeDefinition strength;
        private AttributeDefinition resource;
        private AttributeSet set;

        [SetUp]
        public void SetUp()
        {
            strength = ScriptableObject.CreateInstance<AttributeDefinition>();
            strength.name = "Strength";

            resource = ScriptableObject.CreateInstance<AttributeDefinition>();
            resource.name = "Resource";
            resource.hasMin = true;
            resource.minValue = 0f;
            resource.hasMax = true;
            resource.maxValue = 100f;

            set = new AttributeSet();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(strength);
            Object.DestroyImmediate(resource);
        }

        [Test]
        public void Formula_MatchesChannelOrder()
        {
            set.AddAttribute(strength, 10f);
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 5f, this));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.PercentAdd, 0.2f, this));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.PercentAdd, 0.3f, this));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.PercentMult, 0.5f, this));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.PercentMult, 0.1f, this));

            // (10 + 5) * (1 + 0.2 + 0.3) * (1.5 * 1.1)
            Assert.That(set.GetValue(strength), Is.EqualTo(15f * 1.5f * 1.65f).Within(1e-4f));
            Assert.That(set.GetBaseValue(strength), Is.EqualTo(10f), "modifiers must not touch base");
        }

        [Test]
        public void Override_Wins_AndLastOverrideWinsOverEarlier()
        {
            set.AddAttribute(strength, 10f);
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 100f, this));

            var firstOverride = new object();
            var secondOverride = new object();
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Override, 42f, firstOverride));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Override, 7f, secondOverride));
            Assert.That(set.GetValue(strength), Is.EqualTo(7f));

            set.RemoveModifiersFromSource(secondOverride);
            Assert.That(set.GetValue(strength), Is.EqualTo(42f));

            set.RemoveModifiersFromSource(firstOverride);
            Assert.That(set.GetValue(strength), Is.EqualTo(110f), "removing all overrides restores the formula");
        }

        [Test]
        public void Clamp_AppliesToBaseAndCurrent()
        {
            set.AddAttribute(resource, 250f);
            Assert.That(set.GetBaseValue(resource), Is.EqualTo(100f), "seed base is clamped");

            set.SetBaseValue(resource, -50f);
            Assert.That(set.GetBaseValue(resource), Is.EqualTo(0f));

            set.SetBaseValue(resource, 90f);
            set.AddModifier(resource, new AttributeModifier(ModifierChannel.Add, 999f, this));
            Assert.That(set.GetValue(resource), Is.EqualTo(100f), "current is clamped");
        }

        [Test]
        public void ModifyBaseValue_AppliesDelta()
        {
            set.AddAttribute(resource, 50f);
            set.ModifyBaseValue(resource, -20f);
            Assert.That(set.GetBaseValue(resource), Is.EqualTo(30f));
        }

        [Test]
        public void AttributeChanged_ReportsOldAndNew_AndSkipsNoOps()
        {
            set.AddAttribute(strength, 10f);
            var events = new List<(float oldValue, float newValue)>();
            set.AttributeChanged += (def, oldValue, newValue) => events.Add((oldValue, newValue));

            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 5f, this));
            Assert.That(events, Is.EqualTo(new[] { (10f, 15f) }));

            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 0f, this));
            Assert.That(events.Count, Is.EqualTo(1), "a modifier that changes nothing must not fire");
        }

        [Test]
        public void RemoveModifiersFromSource_SweepsAllAttributes()
        {
            set.AddAttribute(strength, 10f);
            set.AddAttribute(resource, 50f);
            var source = new object();
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 1f, source));
            set.AddModifier(resource, new AttributeModifier(ModifierChannel.Add, 1f, source));
            set.AddModifier(strength, new AttributeModifier(ModifierChannel.Add, 2f, this));

            Assert.That(set.RemoveModifiersFromSource(source), Is.True);
            Assert.That(set.GetValue(strength), Is.EqualTo(12f), "other sources stay applied");
            Assert.That(set.GetValue(resource), Is.EqualTo(50f));
            Assert.That(set.RemoveModifiersFromSource(source), Is.False, "nothing left to remove");
        }

        [Test]
        public void AddAttribute_Twice_ReturnsExistingUnchanged()
        {
            set.AddAttribute(strength, 10f);
            set.AddAttribute(strength, 999f);
            Assert.That(set.GetBaseValue(strength), Is.EqualTo(10f));
        }

        [Test]
        public void MissingAttribute_ReturnsZeroWithWarning()
        {
            Assert.That(set.GetValue(strength), Is.EqualTo(0f));
            Assert.That(set.HasAttribute(strength), Is.False);
        }
    }
}
