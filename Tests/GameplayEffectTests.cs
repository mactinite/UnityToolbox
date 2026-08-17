using System;
using System.Collections.Generic;
using NUnit.Framework;
using toolbox.AbilitySystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace toolbox.Tests
{
    public class GameplayEffectTests
    {
        private AbilitySystemCore core;
        private AttributeDefinition health;

        [SetUp]
        public void SetUp()
        {
            core = new AbilitySystemCore();
            health = TestAssets.Attribute("Health");
            core.Attributes.AddAttribute(health, 100f);
        }

        [TearDown]
        public void TearDown()
        {
            TestAssets.DestroyAll();
            TestTags.DestroyAll();
        }

        [Test]
        public void Instant_ExecutesAgainstBase_PerChannel()
        {
            core.TryApplyEffect(TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, -30f) }));
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(70f));

            core.TryApplyEffect(TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.PercentAdd, 0.5f) }));
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(105f));

            core.TryApplyEffect(TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Override, 42f) }));
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(42f));

            Assert.That(core.ActiveEffects, Is.Empty, "instant effects leave nothing behind");
        }

        [Test]
        public void Duration_ModifiersApply_ThenExpire()
        {
            var removed = new List<ActiveGameplayEffect>();
            core.EffectRemoved += removed.Add;

            core.TryApplyEffect(TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Duration;
                e.duration = new ScalableFloat(5f);
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, 50f) };
            }));

            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(150f));
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(100f), "durational modifiers never touch base");

            core.Tick(4.9f);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(150f));

            core.Tick(0.2f);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f));
            Assert.That(core.ActiveEffects, Is.Empty);
            Assert.That(removed.Count, Is.EqualTo(1));
        }

        [Test]
        public void Duration_GrantedTag_LivesAndDiesWithEffect()
        {
            var buffed = TestTags.Create("Buffed");
            core.TryApplyEffect(TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Duration;
                e.duration = new ScalableFloat(2f);
                e.grantedTags = new GameplayTagContainer(buffed);
            }));

            Assert.That(core.Tags.HasTag(buffed), Is.True);
            core.Tick(2.1f);
            Assert.That(core.Tags.HasTag(buffed), Is.False);
        }

        [Test]
        public void Infinite_PersistsUntilRemoved()
        {
            core.TryApplyEffect(TestAssets.MakeSpec(core, TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, 25f) };
            })), out var active);

            core.Tick(1000f);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(125f));

            Assert.That(core.RemoveEffect(active), Is.True);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f));
            Assert.That(core.RemoveEffect(active), Is.False, "double removal is a no-op");
        }

        [Test]
        public void Periodic_ExecutesPerPeriod_AgainstBase()
        {
            core.TryApplyEffect(TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.isPeriodic = true;
                e.period = new ScalableFloat(1f);
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, -5f) };
            }));

            core.Tick(3.05f);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(85f), "three periods elapsed");
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(85f), "periodic effects add no continuous modifier");
        }

        [Test]
        public void Periodic_ExecuteOnApplication_RunsImmediately()
        {
            var executions = 0;
            core.EffectExecuted += _ => executions++;

            core.TryApplyEffect(TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.isPeriodic = true;
                e.period = new ScalableFloat(1f);
                e.executeOnApplication = true;
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, -5f) };
            }));

            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(95f));
            core.Tick(1.01f);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(90f));
            Assert.That(executions, Is.EqualTo(2));
        }

        [Test]
        public void ApplicationTags_GateApplication()
        {
            var ready = TestTags.Create("Ready");
            var silenced = TestTags.Create("Silenced");

            var needsReady = TestAssets.Effect(e => e.applicationRequiredTags = new GameplayTagContainer(ready));
            Assert.That(core.TryApplyEffect(needsReady), Is.False);

            core.Tags.AddTag(ready);
            Assert.That(core.TryApplyEffect(needsReady), Is.True);

            var notWhileSilenced = TestAssets.Effect(e => e.applicationBlockedTags = new GameplayTagContainer(silenced));
            core.Tags.AddTag(silenced);
            Assert.That(core.TryApplyEffect(notWhileSilenced), Is.False);
        }

        [Test]
        public void RemoveEffectsWithTags_Dispels_ByGrantedTagHierarchy()
        {
            var buff = TestTags.Create("Buff");
            var speed = TestTags.Create("Speed", buff);

            core.TryApplyEffect(TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.grantedTags = new GameplayTagContainer(speed);
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, 50f) };
            }));
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(150f));

            // Dispel anything granting a Buff.* tag.
            core.TryApplyEffect(TestAssets.Effect(e => e.removeEffectsWithTags = new GameplayTagContainer(buff)));

            Assert.That(core.ActiveEffects, Is.Empty);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f));
            Assert.That(core.Tags.HasTag(buff), Is.False);
        }

        [Test]
        public void SameDefinitionTwice_WithoutStacking_IsTwoIndependentEffects()
        {
            var buffed = TestTags.Create("Buffed");
            var definition = TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.grantedTags = new GameplayTagContainer(buffed);
            });

            core.TryApplyEffect(TestAssets.MakeSpec(core, definition), out var first);
            core.TryApplyEffect(TestAssets.MakeSpec(core, definition), out var second);
            Assert.That(core.ActiveEffects.Count, Is.EqualTo(2));

            core.RemoveEffect(first);
            Assert.That(core.Tags.HasTag(buffed), Is.True, "second grant still holds the tag");

            core.RemoveEffect(second);
            Assert.That(core.Tags.HasTag(buffed), Is.False);
        }

        [Test]
        public void OngoingRequiredTags_InhibitAndResume_WhileDurationTicks()
        {
            var stance = TestTags.Create("Stance");
            core.TryApplyEffect(TestAssets.MakeSpec(core, TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Duration;
                e.duration = new ScalableFloat(10f);
                e.ongoingRequiredTags = new GameplayTagContainer(stance);
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, 50f) };
            })), out var active);

            Assert.That(active.IsInhibited, Is.True, "requirement unmet at application");
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f));

            core.Tags.AddTag(stance);
            Assert.That(active.IsInhibited, Is.False);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(150f));

            core.Tags.RemoveTag(stance);
            Assert.That(active.IsInhibited, Is.True);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f));

            core.Tick(10.1f);
            Assert.That(core.ActiveEffects, Is.Empty, "duration ran down while inhibited");
        }

        [Test]
        public void Stacking_CapsRefreshesAndCompounds()
        {
            var definition = TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Duration;
                e.duration = new ScalableFloat(10f);
                e.stacking = EffectStackingPolicy.AggregateByTarget;
                e.maxStacks = 3;
                e.refreshDurationOnStack = true;
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.Add, 10f) };
            });

            core.TryApplyEffect(TestAssets.MakeSpec(core, definition), out var active);
            core.Tick(5f);
            Assert.That(active.RemainingDuration, Is.EqualTo(5f).Within(1e-3f));

            core.TryApplyEffect(TestAssets.MakeSpec(core, definition), out var stacked);
            Assert.That(stacked, Is.SameAs(active), "aggregated onto the existing instance");
            Assert.That(active.StackCount, Is.EqualTo(2));
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(120f));
            Assert.That(active.RemainingDuration, Is.EqualTo(10f), "refreshed on stack");

            core.TryApplyEffect(definition);
            core.TryApplyEffect(definition);
            Assert.That(active.StackCount, Is.EqualTo(3), "capped at max stacks");
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(130f));

            core.Tick(10.1f);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(100f), "expiry removes every stack at once");
        }

        [Test]
        public void Stacking_PercentMult_CompoundsPerStack()
        {
            var definition = TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.stacking = EffectStackingPolicy.AggregateByTarget;
                e.maxStacks = 2;
                e.modifiers = new[] { TestAssets.Mod(health, ModifierChannel.PercentMult, 0.1f) };
            });

            core.TryApplyEffect(definition);
            core.TryApplyEffect(definition);
            Assert.That(core.Attributes.GetValue(health), Is.EqualTo(121f).Within(1e-3f));
        }

        [Test]
        public void ScalableFloat_ScalesWithSpecLevel()
        {
            var definition = TestAssets.Effect(e => e.modifiers = new[]
            {
                new EffectModifierDefinition
                {
                    attribute = health,
                    channel = ModifierChannel.Add,
                    magnitude = new ScalableFloat(5f)
                    {
                        scaleWithLevel = true,
                        levelCurve = AnimationCurve.Linear(0f, 0f, 10f, 10f),
                    },
                },
            });

            core.TryApplyEffect(TestAssets.MakeSpec(core, definition, level: 2f), out _);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(110f), "5 * curve(2) = 10");
        }

        [Test]
        public void AttributeBackedMagnitude_SnapshotVersusLive()
        {
            var power = TestAssets.Attribute("Power");
            core.Attributes.AddAttribute(power, 50f);

            GameplayEffectDefinition Drain(CaptureTiming timing) => TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Infinite;
                e.isPeriodic = true;
                e.period = new ScalableFloat(1f);
                e.modifiers = new[]
                {
                    new EffectModifierDefinition
                    {
                        attribute = health,
                        channel = ModifierChannel.Add,
                        customMagnitude = TestAssets.Magnitude(power, CaptureSource.Source, timing, coefficient: -0.1f),
                    },
                };
            });

            core.TryApplyEffect(TestAssets.MakeSpec(core, Drain(CaptureTiming.OnApplication)), out var snapshot);
            core.Tick(1.01f);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(95f));

            core.Attributes.SetBaseValue(power, 200f);
            core.Tick(1f);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(90f), "snapshot ignores the Power change");
            core.RemoveEffect(snapshot);

            core.TryApplyEffect(TestAssets.MakeSpec(core, Drain(CaptureTiming.OnEvaluation)), out _);
            core.Tick(1.01f);
            Assert.That(core.Attributes.GetBaseValue(health), Is.EqualTo(70f), "live capture reads Power = 200");
        }
    }

    /// <summary>Builds ability-system assets for tests and cleans them up afterwards.</summary>
    internal static class TestAssets
    {
        private static readonly List<Object> created = new();

        public static AttributeDefinition Attribute(string name)
        {
            var attribute = ScriptableObject.CreateInstance<AttributeDefinition>();
            attribute.name = name;
            created.Add(attribute);
            return attribute;
        }

        public static GameplayEffectDefinition Effect(Action<GameplayEffectDefinition> configure = null)
        {
            var effect = ScriptableObject.CreateInstance<GameplayEffectDefinition>();
            configure?.Invoke(effect);
            created.Add(effect);
            return effect;
        }

        public static AttributeBackedMagnitude Magnitude(
            AttributeDefinition attribute, CaptureSource from, CaptureTiming when, float coefficient)
        {
            var magnitude = ScriptableObject.CreateInstance<AttributeBackedMagnitude>();
            magnitude.attribute = attribute;
            magnitude.captureFrom = from;
            magnitude.captureWhen = when;
            magnitude.coefficient = new ScalableFloat(coefficient);
            created.Add(magnitude);
            return magnitude;
        }

        public static EffectModifierDefinition Mod(AttributeDefinition attribute, ModifierChannel channel, float value) =>
            new() { attribute = attribute, channel = channel, magnitude = new ScalableFloat(value) };

        public static GameplayEffectSpec MakeSpec(AbilitySystemCore source, GameplayEffectDefinition definition, float level = -1f) =>
            source.MakeEffectSpec(definition, level);

        public static void Track(Object asset) => created.Add(asset);

        public static void DestroyAll()
        {
            foreach (var asset in created)
                Object.DestroyImmediate(asset);
            created.Clear();
        }
    }
}
