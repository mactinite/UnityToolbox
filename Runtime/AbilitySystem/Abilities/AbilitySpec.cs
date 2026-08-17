namespace toolbox.AbilitySystem
{
    /// <summary>Cooldown state for UI. <see cref="Remaining"/> is positive infinity for an infinite cooldown.</summary>
    public struct AbilityCooldown
    {
        public float Remaining;
        public float Total;
        public bool IsActive => Remaining > 0f;
    }

    /// <summary>
    /// The runtime half of an ability: one instance per grant, holding all mutable state (shared
    /// <see cref="GameplayAbilityDefinition"/> assets must stay stateless). Lifecycle:
    /// <see cref="CanActivate"/> → <see cref="OnActivate"/> → <see cref="OnTick"/> each frame while
    /// active → <see cref="End"/>/<see cref="Cancel"/> → <see cref="OnEnd"/>. An ability that
    /// finishes instantly simply calls <see cref="End"/> inside <see cref="OnActivate"/>.
    /// </summary>
    public abstract class AbilitySpec
    {
        protected AbilitySpec(GameplayAbilityDefinition definition, AbilitySystemCore owner)
        {
            Definition = definition;
            Owner = owner;
            Level = owner.Level;
        }

        public GameplayAbilityDefinition Definition { get; }

        public AbilitySystemCore Owner { get; }

        public float Level { get; set; }

        public bool IsActive { get; private set; }

        /// <summary>Whether the most recent end was a cancellation.</summary>
        public bool WasCancelled { get; private set; }

        /// <summary>Pay cost and start cooldown automatically during activation. Override to false for
        /// abilities that commit manually mid-execution (e.g. pay on hit).</summary>
        protected virtual bool CommitOnActivate => true;

        /// <summary>Whether activation would succeed right now: not active, tag requirements met,
        /// not blocked by an active ability, off cooldown, and the cost is affordable.</summary>
        public virtual bool CanActivate() =>
            !IsActive
            && Owner.Tags.HasAll(Definition.ActivationRequiredTags)
            && Owner.Tags.HasNone(Definition.ActivationBlockedTags)
            && !IsBlocked()
            && !IsOnCooldown()
            && CheckCost();

        /// <summary>True while any cooldown tag from the cooldown effect is on the owner. O(1).</summary>
        public bool IsOnCooldown() =>
            Definition.Cooldown != null && Owner.Tags.HasAny(Definition.Cooldown.GrantedTags);

        /// <summary>Cooldown remaining/total for UI. Scans active effects only when actually on cooldown.</summary>
        public AbilityCooldown GetCooldown()
        {
            var cooldown = Definition.Cooldown;
            if (cooldown == null || !Owner.Tags.HasAny(cooldown.GrantedTags))
                return default;

            var result = new AbilityCooldown();
            var activeEffects = Owner.ActiveEffects;
            for (var i = 0; i < activeEffects.Count; i++)
            {
                var active = activeEffects[i];
                if (!AbilitySystemCore.GrantsAnyOf(active.Spec.Definition.GrantedTags, cooldown.GrantedTags))
                    continue;

                if (active.RemainingDuration > result.Remaining)
                {
                    result.Remaining = active.RemainingDuration;
                    result.Total = active.TotalDuration;
                }
            }

            return result;
        }

        /// <summary>
        /// Whether the owner can afford the cost right now, simulating the full instant application —
        /// every channel, every row — against each attribute's minimum (0 when it has none).
        /// </summary>
        public bool CheckCost()
        {
            var cost = Definition.Cost;
            if (cost == null || cost.DurationPolicy != EffectDurationPolicy.Instant)
                return true;

            var spec = Owner.MakeEffectSpec(cost, Level, instigator: this);
            spec.Target = Owner;

            var modifiers = cost.Modifiers;
            for (var i = 0; i < modifiers.Count; i++)
            {
                var attribute = modifiers[i].attribute;
                if (attribute == null)
                    continue;

                // Predict each attribute once, applying every row that touches it in order.
                var alreadyPredicted = false;
                for (var j = 0; j < i; j++)
                    alreadyPredicted |= modifiers[j].attribute == attribute;
                if (alreadyPredicted)
                    continue;

                var value = Owner.Attributes.GetBaseValue(attribute);
                for (var j = 0; j < modifiers.Count; j++)
                {
                    if (modifiers[j].attribute != attribute)
                        continue;

                    var magnitude = spec.EvaluateModifierMagnitude(j);
                    switch (modifiers[j].channel)
                    {
                        case ModifierChannel.Add:
                            value += magnitude;
                            break;
                        case ModifierChannel.PercentAdd:
                        case ModifierChannel.PercentMult:
                            value *= 1f + magnitude;
                            break;
                        case ModifierChannel.Override:
                            value = magnitude;
                            break;
                    }
                }

                var floor = attribute.HasMin ? attribute.MinValue : 0f;
                if (value < floor)
                    return false;
            }

            return true;
        }

        /// <summary>Ends the ability normally. Safe to call from inside <see cref="OnActivate"/>/<see cref="OnTick"/>; no-op when inactive.</summary>
        public void End() => EndInternal(cancelled: false);

        /// <summary>Ends the ability as a cancellation (interrupted, owner died). No-op when inactive.</summary>
        public void Cancel() => EndInternal(cancelled: true);

        /// <summary>Checks, cancels conflicting abilities, grants activation tags and commits. Core-driven.</summary>
        internal bool BeginActivation()
        {
            if (!CanActivate())
                return false;

            Owner.CancelAbilitiesWithTags(Definition.CancelAbilitiesWithTags);

            IsActive = true;
            WasCancelled = false;
            AddTags(Owner.Tags, Definition.ActivationOwnedTags);
            AddTags(Owner.BlockedAbilityTags, Definition.BlockAbilitiesWithTags);

            if (CommitOnActivate && !Commit())
            {
                EndInternal(cancelled: true);
                return false;
            }

            return true;
        }

        internal void RunActivation() => OnActivate();

        internal void Tick(float deltaTime)
        {
            if (IsActive)
                OnTick(deltaTime);
        }

        internal bool IsBlocked()
        {
            var tags = Definition.AbilityTags.Tags;
            for (var i = 0; i < tags.Count; i++)
            {
                if (Owner.BlockedAbilityTags.HasTagOrAncestor(tags[i]))
                    return true;
            }

            return false;
        }

        /// <summary>Pays the cost and starts the cooldown. Returns false (paying nothing) when unaffordable.</summary>
        protected bool Commit()
        {
            if (!CheckCost())
                return false;

            if (Definition.Cost != null)
                Owner.TryApplyEffect(Owner.MakeEffectSpec(Definition.Cost, Level, instigator: this), out _);
            if (Definition.Cooldown != null)
                Owner.TryApplyEffect(Owner.MakeEffectSpec(Definition.Cooldown, Level, instigator: this), out _);

            return true;
        }

        /// <summary>The ability's behaviour. Runs after cost/cooldown commit; call <see cref="End"/> when done.</summary>
        protected abstract void OnActivate();

        /// <summary>Called every frame while active with scaled delta time.</summary>
        protected virtual void OnTick(float deltaTime)
        {
        }

        /// <summary>Cleanup hook; runs before the owner's AbilityEnded event.</summary>
        protected virtual void OnEnd(bool cancelled)
        {
        }

        private void EndInternal(bool cancelled)
        {
            if (!IsActive)
                return;

            IsActive = false;
            WasCancelled = cancelled;
            RemoveTags(Owner.Tags, Definition.ActivationOwnedTags);
            RemoveTags(Owner.BlockedAbilityTags, Definition.BlockAbilitiesWithTags);
            OnEnd(cancelled);
            Owner.NotifyAbilityEnded(this);
        }

        private static void AddTags(CountedTagSet set, GameplayTagContainer tags)
        {
            for (var i = 0; i < tags.Tags.Count; i++)
                set.AddTag(tags.Tags[i]);
        }

        private static void RemoveTags(CountedTagSet set, GameplayTagContainer tags)
        {
            for (var i = 0; i < tags.Tags.Count; i++)
                set.RemoveTag(tags.Tags[i]);
        }
    }
}
