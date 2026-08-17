using System.Collections.Generic;
using NUnit.Framework;
using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Tests
{
    /// <summary>
    /// Also documents THE extension pattern: subclass the definition with authored data and a
    /// CreateSpec override, subclass the spec with behaviour.
    /// </summary>
    internal sealed class TestAbilityDefinition : GameplayAbilityDefinition
    {
        public bool endImmediately;

        public override AbilitySpec CreateSpec(AbilitySystemCore owner) => new TestAbilitySpec(this, owner);
    }

    internal sealed class TestAbilitySpec : AbilitySpec
    {
        public readonly List<string> log = new();
        public float tickedTime;

        public TestAbilitySpec(TestAbilityDefinition definition, AbilitySystemCore owner)
            : base(definition, owner)
        {
        }

        protected override void OnActivate()
        {
            log.Add("activate");
            if (((TestAbilityDefinition)Definition).endImmediately)
                End();
        }

        protected override void OnTick(float deltaTime)
        {
            log.Add("tick");
            tickedTime += deltaTime;
        }

        protected override void OnEnd(bool cancelled) => log.Add(cancelled ? "cancel" : "end");
    }

    public class AbilityTests
    {
        private AbilitySystemCore core;
        private AttributeDefinition mana;

        [SetUp]
        public void SetUp()
        {
            core = new AbilitySystemCore();
            mana = TestAssets.Attribute("Mana");
            core.Attributes.AddAttribute(mana, 30f);
        }

        [TearDown]
        public void TearDown()
        {
            TestAssets.DestroyAll();
            TestTags.DestroyAll();
        }

        private static TestAbilityDefinition Ability(System.Action<TestAbilityDefinition> configure = null)
        {
            var definition = ScriptableObject.CreateInstance<TestAbilityDefinition>();
            configure?.Invoke(definition);
            TestAssets.Track(definition);
            return definition;
        }

        [Test]
        public void GrantFindRevoke()
        {
            var definition = Ability();
            var spec = core.GrantAbility(definition);

            Assert.That(core.FindAbility(definition), Is.SameAs(spec));
            Assert.That(core.GrantedAbilities, Has.Count.EqualTo(1));

            core.TryActivateAbility(spec);
            Assert.That(core.RevokeAbility(spec), Is.True);
            Assert.That(((TestAbilitySpec)spec).log, Does.Contain("cancel"), "revoking an active ability cancels it");
            Assert.That(core.GrantedAbilities, Is.Empty);
            Assert.That(core.RevokeAbility(spec), Is.False);
        }

        [Test]
        public void Lifecycle_ActivateTickEnd_InOrder()
        {
            var spec = (TestAbilitySpec)core.GrantAbility(Ability());
            var events = new List<string>();
            core.AbilityActivated += _ => events.Add("activated-event");
            core.AbilityEnded += _ => events.Add("ended-event");

            Assert.That(core.TryActivateAbility(spec), Is.True);
            Assert.That(spec.IsActive, Is.True);
            Assert.That(core.TryActivateAbility(spec), Is.False, "already active");

            core.Tick(0.5f);
            core.Tick(0.25f);
            spec.End();

            Assert.That(spec.log, Is.EqualTo(new[] { "activate", "tick", "tick", "end" }));
            Assert.That(spec.tickedTime, Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(spec.IsActive, Is.False);
            Assert.That(spec.WasCancelled, Is.False);
            Assert.That(events, Is.EqualTo(new[] { "activated-event", "ended-event" }));
        }

        [Test]
        public void InstantAbility_EndsInsideOnActivate_EventOrderPreserved()
        {
            var spec = (TestAbilitySpec)core.GrantAbility(Ability(a => a.endImmediately = true));
            var events = new List<string>();
            core.AbilityActivated += _ => events.Add("activated");
            core.AbilityEnded += _ => events.Add("ended");

            Assert.That(core.TryActivateAbility(spec), Is.True);
            Assert.That(spec.IsActive, Is.False);
            Assert.That(events, Is.EqualTo(new[] { "activated", "ended" }));

            core.Tick(1f);
            Assert.That(spec.log, Does.Not.Contain("tick"), "inactive abilities don't tick");
        }

        [Test]
        public void Cancel_ReportsCancellation()
        {
            var spec = (TestAbilitySpec)core.GrantAbility(Ability());
            core.TryActivateAbility(spec);
            spec.Cancel();

            Assert.That(spec.WasCancelled, Is.True);
            Assert.That(spec.log, Does.Contain("cancel"));
        }

        [Test]
        public void Cost_GatesAndPaysExactlyOnce()
        {
            var tooExpensive = TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(mana, ModifierChannel.Add, -50f) });
            var affordable = TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(mana, ModifierChannel.Add, -20f) });

            var blocked = core.GrantAbility(Ability(a => a.cost = tooExpensive));
            Assert.That(blocked.CheckCost(), Is.False);
            Assert.That(core.TryActivateAbility(blocked), Is.False);
            Assert.That(core.Attributes.GetBaseValue(mana), Is.EqualTo(30f), "a refused activation pays nothing");

            var paid = core.GrantAbility(Ability(a => a.cost = affordable));
            Assert.That(core.TryActivateAbility(paid), Is.True);
            Assert.That(core.Attributes.GetBaseValue(mana), Is.EqualTo(10f), "cost paid exactly once");
        }

        [Test]
        public void Cost_SimulatesNonAddChannels()
        {
            // A cost that halves mana is always affordable; one that overrides below zero never is.
            var halve = TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(mana, ModifierChannel.PercentAdd, -0.5f) });
            var overdraw = TestAssets.Effect(e =>
                e.modifiers = new[] { TestAssets.Mod(mana, ModifierChannel.Override, -1f) });

            Assert.That(core.GrantAbility(Ability(a => a.cost = halve)).CheckCost(), Is.True);
            Assert.That(core.GrantAbility(Ability(a => a.cost = overdraw)).CheckCost(), Is.False);
        }

        [Test]
        public void Cooldown_GatesUntilItsEffectExpires()
        {
            var cooldownTag = TestTags.Create("Cooldown.Test");
            var cooldownEffect = TestAssets.Effect(e =>
            {
                e.durationPolicy = EffectDurationPolicy.Duration;
                e.duration = new ScalableFloat(3f);
                e.grantedTags = new GameplayTagContainer(cooldownTag);
            });

            var spec = core.GrantAbility(Ability(a =>
            {
                a.endImmediately = true;
                a.cooldown = cooldownEffect;
            }));

            Assert.That(core.TryActivateAbility(spec), Is.True);
            Assert.That(spec.IsOnCooldown(), Is.True);
            Assert.That(core.TryActivateAbility(spec), Is.False);

            var cooldown = spec.GetCooldown();
            Assert.That(cooldown.Remaining, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(cooldown.Total, Is.EqualTo(3f).Within(1e-3f));

            core.Tick(1f);
            Assert.That(spec.GetCooldown().Remaining, Is.EqualTo(2f).Within(1e-3f));

            core.Tick(2.1f);
            Assert.That(spec.IsOnCooldown(), Is.False);
            Assert.That(spec.GetCooldown().IsActive, Is.False);
            Assert.That(core.TryActivateAbility(spec), Is.True);
        }

        [Test]
        public void ActivationTags_RequireAndBlock()
        {
            var armed = TestTags.Create("Armed");
            var stunned = TestTags.Create("Stunned");

            var spec = core.GrantAbility(Ability(a =>
            {
                a.activationRequiredTags = new GameplayTagContainer(armed);
                a.activationBlockedTags = new GameplayTagContainer(stunned);
            }));

            Assert.That(core.TryActivateAbility(spec), Is.False, "required tag missing");

            core.Tags.AddTag(armed);
            core.Tags.AddTag(stunned);
            Assert.That(core.TryActivateAbility(spec), Is.False, "blocked tag present");

            core.Tags.RemoveTag(stunned);
            Assert.That(core.TryActivateAbility(spec), Is.True);
        }

        [Test]
        public void ActivationOwnedTags_PresentOnlyWhileActive()
        {
            var sprinting = TestTags.Create("Sprinting");
            var spec = core.GrantAbility(Ability(a =>
                a.activationOwnedTags = new GameplayTagContainer(sprinting)));

            Assert.That(core.Tags.HasTag(sprinting), Is.False);
            core.TryActivateAbility(spec);
            Assert.That(core.Tags.HasTag(sprinting), Is.True);
            spec.End();
            Assert.That(core.Tags.HasTag(sprinting), Is.False);
        }

        [Test]
        public void BlockAbilitiesWithTags_BlocksByHierarchy_WhileActive()
        {
            var attack = TestTags.Create("Attack");
            var heavy = TestTags.Create("Heavy", attack);

            var blocker = core.GrantAbility(Ability(a =>
                a.blockAbilitiesWithTags = new GameplayTagContainer(attack)));
            var heavyAttack = core.GrantAbility(Ability(a =>
                a.abilityTags = new GameplayTagContainer(heavy)));

            core.TryActivateAbility(blocker);
            Assert.That(core.TryActivateAbility(heavyAttack), Is.False, "blocking 'Attack' blocks 'Attack.Heavy'");

            blocker.End();
            Assert.That(core.TryActivateAbility(heavyAttack), Is.True);
        }

        [Test]
        public void CancelAbilitiesWithTags_OnActivation()
        {
            var channel = TestTags.Create("Channel");

            var channelled = (TestAbilitySpec)core.GrantAbility(Ability(a =>
                a.abilityTags = new GameplayTagContainer(channel)));
            var interrupter = core.GrantAbility(Ability(a =>
                a.cancelAbilitiesWithTags = new GameplayTagContainer(channel)));

            core.TryActivateAbility(channelled);
            core.TryActivateAbility(interrupter);

            Assert.That(channelled.IsActive, Is.False);
            Assert.That(channelled.WasCancelled, Is.True);
            Assert.That(interrupter.IsActive, Is.True);
        }

        [Test]
        public void CancelAllAbilities_CancelsEveryActiveSpec()
        {
            var first = core.GrantAbility(Ability());
            var second = core.GrantAbility(Ability());
            core.TryActivateAbility(first);
            core.TryActivateAbility(second);

            core.CancelAllAbilities();

            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.False);
            Assert.That(first.WasCancelled, Is.True);
        }
    }
}
