using System;
using System.Collections.Generic;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// The plain-C# heart of the ability system: one per owner, holding its tags, attributes and
    /// active effects, and driving them from <see cref="Tick"/>. It has no engine lifecycle of its
    /// own — <see cref="AbilitySystemComponent"/> is the standard MonoBehaviour host, and tests drive
    /// a core directly.
    /// </summary>
    public sealed class AbilitySystemCore
    {
        private readonly List<ActiveGameplayEffect> activeEffects = new();
        private readonly List<ActiveGameplayEffect> effectTickBuffer = new();
        private readonly List<AbilitySpec> grantedAbilities = new();
        private readonly List<AbilitySpec> abilityTickBuffer = new();
        private bool inhibitionCheckRunning;
        private bool inhibitionCheckPending;

        public AbilitySystemCore()
        {
            Tags.Changed += OnTagsChanged;
        }

        /// <summary>Tags this owner currently has: loose grants, effect-granted, and ability-owned.</summary>
        public CountedTagSet Tags { get; } = new();

        /// <summary>Tags of abilities currently blocked, populated by active abilities' block lists.</summary>
        public CountedTagSet BlockedAbilityTags { get; } = new();

        public AttributeSet Attributes { get; } = new();

        public IReadOnlyList<ActiveGameplayEffect> ActiveEffects => activeEffects;

        public IReadOnlyList<AbilitySpec> GrantedAbilities => grantedAbilities;

        /// <summary>Default level for specs made by this owner.</summary>
        public float Level { get; set; } = 1f;

        /// <summary>Opaque host reference; <see cref="AbilitySystemComponent"/> sets itself here so
        /// ability specs can reach the scene.</summary>
        public object Context { get; set; }

        /// <summary>Raised when a Duration/Infinite effect becomes active on this owner.</summary>
        public event Action<ActiveGameplayEffect> EffectApplied;

        /// <summary>Raised when an active effect expires or is removed.</summary>
        public event Action<ActiveGameplayEffect> EffectRemoved;

        /// <summary>Raised for every execution against base values: each Instant application and each periodic tick.</summary>
        public event Action<GameplayEffectSpec> EffectExecuted;

        /// <summary>Raised after an ability successfully activates, before its OnActivate runs.</summary>
        public event Action<AbilitySpec> AbilityActivated;

        /// <summary>Raised after an ability ends; <see cref="AbilitySpec.WasCancelled"/> tells which way.</summary>
        public event Action<AbilitySpec> AbilityEnded;

        /// <summary>
        /// Advances the system: effect durations run down, periodic effects execute, expired effects
        /// are removed, then active abilities tick. Call once per frame with scaled delta time.
        /// </summary>
        public void Tick(float deltaTime)
        {
            effectTickBuffer.Clear();
            effectTickBuffer.AddRange(activeEffects);

            for (var i = 0; i < effectTickBuffer.Count; i++)
            {
                var active = effectTickBuffer[i];
                if (active.isRemoved)
                    continue;

                active.Tick(deltaTime, out var expired, out var periodicExecutions);

                for (var e = 0; e < periodicExecutions && !active.isRemoved; e++)
                    ExecuteAgainstBase(active.Spec, active.StackCount);

                if (expired && !active.isRemoved)
                    RemoveEffect(active);
            }

            abilityTickBuffer.Clear();
            abilityTickBuffer.AddRange(grantedAbilities);
            for (var i = 0; i < abilityTickBuffer.Count; i++)
                abilityTickBuffer[i].Tick(deltaTime);
        }

        /// <summary>
        /// Grants the ability, creating its per-owner spec. Level -1 uses <see cref="Level"/>.
        /// Granting the same definition twice yields two independent specs.
        /// </summary>
        public AbilitySpec GrantAbility(GameplayAbilityDefinition definition, float level = -1f)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var spec = definition.CreateSpec(this);
            if (spec == null)
                throw new InvalidOperationException($"{definition.name}.CreateSpec returned null.");

            if (level >= 0f)
                spec.Level = level;
            grantedAbilities.Add(spec);
            return spec;
        }

        /// <summary>Revokes a granted ability, cancelling it first if active. False if not granted.</summary>
        public bool RevokeAbility(AbilitySpec spec)
        {
            if (spec == null || !grantedAbilities.Remove(spec))
                return false;

            spec.Cancel();
            return true;
        }

        /// <summary>The first granted spec of this definition, or null.</summary>
        public AbilitySpec FindAbility(GameplayAbilityDefinition definition)
        {
            for (var i = 0; i < grantedAbilities.Count; i++)
            {
                if (grantedAbilities[i].Definition == definition)
                    return grantedAbilities[i];
            }

            return null;
        }

        /// <summary>Activates the granted spec of this definition, if any. See <see cref="TryActivateAbility(AbilitySpec)"/>.</summary>
        public bool TryActivateAbility(GameplayAbilityDefinition definition) =>
            TryActivateAbility(FindAbility(definition));

        /// <summary>
        /// Activates a granted ability: checks pass, conflicting abilities are cancelled, activation
        /// tags are granted, cost and cooldown commit, then the ability's behaviour runs.
        /// </summary>
        public bool TryActivateAbility(AbilitySpec spec)
        {
            if (spec == null || !grantedAbilities.Contains(spec) || !spec.BeginActivation())
                return false;

            AbilityActivated?.Invoke(spec);
            spec.RunActivation();
            return true;
        }

        /// <summary>Cancels the active spec of this definition. False when not granted or not active.</summary>
        public bool CancelAbility(GameplayAbilityDefinition definition)
        {
            var spec = FindAbility(definition);
            if (spec == null || !spec.IsActive)
                return false;

            spec.Cancel();
            return true;
        }

        /// <summary>Cancels every active ability whose ability tags match any query in <paramref name="tags"/>.</summary>
        public void CancelAbilitiesWithTags(GameplayTagContainer tags)
        {
            if (tags == null || tags.IsEmpty)
                return;

            // Cancelling never removes from the granted list, so direct (clamped, backwards)
            // iteration is safe even when OnEnd callbacks grant or revoke re-entrantly.
            for (var i = grantedAbilities.Count - 1; i >= 0; i--)
            {
                if (i >= grantedAbilities.Count)
                    continue;

                var spec = grantedAbilities[i];
                if (spec.IsActive && GrantsAnyOf(spec.Definition.AbilityTags, tags))
                    spec.Cancel();
            }
        }

        public void CancelAllAbilities()
        {
            for (var i = grantedAbilities.Count - 1; i >= 0; i--)
            {
                if (i >= grantedAbilities.Count)
                    continue;

                grantedAbilities[i].Cancel();
            }
        }

        internal void NotifyAbilityEnded(AbilitySpec spec) => AbilityEnded?.Invoke(spec);

        /// <summary>Creates a spec with this owner as the source. Level -1 uses <see cref="Level"/>.</summary>
        public GameplayEffectSpec MakeEffectSpec(GameplayEffectDefinition definition, float level = -1f, object instigator = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return new GameplayEffectSpec(definition, this, level < 0f ? Level : level) { Instigator = instigator };
        }

        /// <summary>Whether this owner's tags satisfy the spec's application requirements.</summary>
        public bool CanApplyEffect(GameplayEffectSpec spec) =>
            spec != null
            && Tags.HasAll(spec.Definition.ApplicationRequiredTags)
            && Tags.HasNone(spec.Definition.ApplicationBlockedTags);

        /// <summary>Convenience for self-applied definitions with no extra context.</summary>
        public bool TryApplyEffect(GameplayEffectDefinition definition, float level = -1f) =>
            TryApplyEffect(MakeEffectSpec(definition, level), out _);

        /// <summary>
        /// Applies the spec to this owner. Instant effects execute immediately and return with a null
        /// <paramref name="active"/>; Duration/Infinite effects become (or stack onto) an active effect.
        /// Returns false when application tag requirements are unmet.
        /// </summary>
        public bool TryApplyEffect(GameplayEffectSpec spec, out ActiveGameplayEffect active)
        {
            active = null;
            if (spec == null || !CanApplyEffect(spec))
                return false;

            spec.Target = this;
            var definition = spec.Definition;

            RemoveEffectsWithTags(definition.RemoveEffectsWithTags);
            spec.RunOnApplicationCaptures();

            if (definition.DurationPolicy == EffectDurationPolicy.Instant)
            {
                ExecuteAgainstBase(spec, 1);
                return true;
            }

            if (definition.Stacking == EffectStackingPolicy.AggregateByTarget
                && TryFindActiveEffect(definition, out var existing))
            {
                AddStack(existing);
                active = existing;
                return true;
            }

            active = new ActiveGameplayEffect(spec);
            activeEffects.Add(active);

            if (!definition.IsPeriodic)
            {
                var modifiers = definition.Modifiers;
                active.snapshotMagnitudes = new float[modifiers.Count];
                for (var i = 0; i < modifiers.Count; i++)
                    active.snapshotMagnitudes[i] = spec.EvaluateModifierMagnitude(i);
            }

            if (!definition.OngoingRequiredTags.IsEmpty && !Tags.HasAll(definition.OngoingRequiredTags))
            {
                active.IsInhibited = true;
            }
            else
            {
                ApplyOngoing(active);
                if (definition.IsPeriodic && definition.ExecuteOnApplication)
                    ExecuteAgainstBase(spec, active.StackCount);
            }

            EffectApplied?.Invoke(active);
            return true;
        }

        /// <summary>Removes an active effect, releasing its modifiers and granted tags. False if not active.</summary>
        public bool RemoveEffect(ActiveGameplayEffect active)
        {
            if (active == null || active.isRemoved || !activeEffects.Remove(active))
                return false;

            active.isRemoved = true;
            if (!active.IsInhibited)
                RemoveOngoing(active);

            EffectRemoved?.Invoke(active);
            return true;
        }

        /// <summary>Removes every active effect whose granted tags match any query in <paramref name="tags"/> (a dispel).</summary>
        public int RemoveEffectsWithTags(GameplayTagContainer tags)
        {
            if (tags == null || tags.IsEmpty)
                return 0;

            var removed = 0;
            for (var i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (i >= activeEffects.Count)
                    continue; // re-entrant removals shrank the list

                var active = activeEffects[i];
                if (GrantsAnyOf(active.Spec.Definition.GrantedTags, tags) && RemoveEffect(active))
                    removed++;
            }

            return removed;
        }

        internal static bool GrantsAnyOf(GameplayTagContainer granted, GameplayTagContainer queries)
        {
            for (var i = 0; i < queries.Tags.Count; i++)
            {
                if (granted.ContainsMatch(queries.Tags[i]))
                    return true;
            }

            return false;
        }

        private bool TryFindActiveEffect(GameplayEffectDefinition definition, out ActiveGameplayEffect found)
        {
            found = null;
            for (var i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].Spec.Definition == definition)
                {
                    found = activeEffects[i];
                    return true;
                }
            }

            return false;
        }

        private void AddStack(ActiveGameplayEffect active)
        {
            var definition = active.Spec.Definition;

            // Refresh applies even at max stacks, matching the usual "re-applying a dot restarts it".
            if (definition.RefreshDurationOnStack)
                active.RemainingDuration = active.TotalDuration;
            if (definition.ResetPeriodOnStack)
                active.timeUntilPeriodTick = active.Spec.EvaluatePeriod();

            if (active.StackCount >= definition.MaxStacks)
                return;

            active.StackCount++;

            // One more application of the continuous modifier list; stacks reuse the original snapshot.
            if (!definition.IsPeriodic && !active.IsInhibited)
                AddModifierRows(active, 1);
        }

        private void ApplyOngoing(ActiveGameplayEffect active)
        {
            var definition = active.Spec.Definition;
            var granted = definition.GrantedTags.Tags;
            for (var i = 0; i < granted.Count; i++)
                Tags.AddTag(granted[i]);

            if (!definition.IsPeriodic)
                AddModifierRows(active, active.StackCount);
        }

        private void RemoveOngoing(ActiveGameplayEffect active)
        {
            var granted = active.Spec.Definition.GrantedTags.Tags;
            for (var i = 0; i < granted.Count; i++)
                Tags.RemoveTag(granted[i]);

            Attributes.RemoveModifiersFromSource(active);
        }

        private void AddModifierRows(ActiveGameplayEffect active, int times)
        {
            var modifiers = active.Spec.Definition.Modifiers;
            for (var t = 0; t < times; t++)
            {
                for (var i = 0; i < modifiers.Count; i++)
                {
                    var row = modifiers[i];
                    Attributes.AddModifier(row.attribute,
                        new AttributeModifier(row.channel, active.snapshotMagnitudes[i], active));
                }
            }
        }

        /// <summary>One execution against base values — an Instant application or one periodic tick.</summary>
        private void ExecuteAgainstBase(GameplayEffectSpec spec, int stacks)
        {
            var modifiers = spec.Definition.Modifiers;
            for (var t = 0; t < stacks; t++)
            {
                for (var i = 0; i < modifiers.Count; i++)
                {
                    var row = modifiers[i];
                    var magnitude = spec.EvaluateModifierMagnitude(i);
                    switch (row.channel)
                    {
                        case ModifierChannel.Add:
                            Attributes.ModifyBaseValue(row.attribute, magnitude);
                            break;
                        case ModifierChannel.PercentAdd:
                        case ModifierChannel.PercentMult:
                            Attributes.SetBaseValue(row.attribute, Attributes.GetBaseValue(row.attribute) * (1f + magnitude));
                            break;
                        case ModifierChannel.Override:
                            Attributes.SetBaseValue(row.attribute, magnitude);
                            break;
                    }
                }
            }

            EffectExecuted?.Invoke(spec);
        }

        /// <summary>
        /// Re-evaluates ongoing tag requirements whenever owned tags change. Toggling an effect's
        /// granted tags can cascade into further changes, so this loops until stable (bounded, since
        /// each effect can only flip so many times before the tag set stops changing).
        /// </summary>
        private void OnTagsChanged()
        {
            inhibitionCheckPending = true;
            if (inhibitionCheckRunning)
                return;

            inhibitionCheckRunning = true;
            try
            {
                var guard = 0;
                while (inhibitionCheckPending && guard++ < 16)
                {
                    inhibitionCheckPending = false;
                    UpdateInhibition();
                }
            }
            finally
            {
                inhibitionCheckRunning = false;
            }
        }

        private void UpdateInhibition()
        {
            for (var i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (i >= activeEffects.Count)
                    continue;

                var active = activeEffects[i];
                var required = active.Spec.Definition.OngoingRequiredTags;
                if (required.IsEmpty)
                    continue;

                var shouldFunction = Tags.HasAll(required);
                if (shouldFunction == !active.IsInhibited)
                    continue;

                active.IsInhibited = !shouldFunction;
                if (shouldFunction)
                    ApplyOngoing(active);
                else
                    RemoveOngoing(active);
            }
        }
    }
}
