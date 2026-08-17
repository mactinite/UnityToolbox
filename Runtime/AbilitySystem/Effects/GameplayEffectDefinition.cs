using System.Collections.Generic;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>How long an effect's influence lasts.</summary>
    public enum EffectDurationPolicy
    {
        /// <summary>Executes once against base values and leaves nothing behind (damage, a cost).</summary>
        Instant,

        /// <summary>Active for an authored duration, then removed (a timed buff, a cooldown).</summary>
        Duration,

        /// <summary>Active until removed explicitly (an equip bonus, a toggled stance).</summary>
        Infinite,
    }

    /// <summary>Whether repeat applications of the same effect combine.</summary>
    public enum EffectStackingPolicy
    {
        /// <summary>Each application is an independent active effect.</summary>
        None,

        /// <summary>Re-applying while active adds a stack on the existing instance (up to MaxStacks).</summary>
        AggregateByTarget,
    }

    /// <summary>
    /// The authored description of one gameplay effect — the workhorse of the system: buffs, debuffs,
    /// damage, costs and cooldowns are all effects. Instant effects execute against base values;
    /// Duration/Infinite effects either contribute continuous modifiers (snapshot at application) or,
    /// when periodic, execute against base values every period. All runtime state lives in
    /// <see cref="GameplayEffectSpec"/>/<see cref="ActiveGameplayEffect"/> — never on this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Gameplay Effect", fileName = "GE_New")]
    public class GameplayEffectDefinition : ScriptableObject
    {
        [SerializeField] internal EffectDurationPolicy durationPolicy = EffectDurationPolicy.Instant;

        [Tooltip("Lifetime in seconds (Duration policy only).")]
        [SerializeField] internal ScalableFloat duration = new(1f);

        [Tooltip("Execute the modifiers against base values every period instead of applying them continuously.")]
        [SerializeField] internal bool isPeriodic;
        [SerializeField] internal ScalableFloat period = new(1f);
        [Tooltip("Also execute immediately when the effect is applied.")]
        [SerializeField] internal bool executeOnApplication;

        [SerializeField] internal EffectModifierDefinition[] modifiers = { };

        [Tooltip("Tags the target owns while this effect is active (not granted by Instant effects).")]
        [SerializeField] internal GameplayTagContainer grantedTags = new();
        [Tooltip("The target must own all of these for the effect to apply.")]
        [SerializeField] internal GameplayTagContainer applicationRequiredTags = new();
        [Tooltip("The effect will not apply while the target owns any of these.")]
        [SerializeField] internal GameplayTagContainer applicationBlockedTags = new();
        [Tooltip("While active, the effect only functions while the target owns all of these; otherwise it is inhibited (duration still ticks).")]
        [SerializeField] internal GameplayTagContainer ongoingRequiredTags = new();
        [Tooltip("Applying this effect removes active effects whose granted tags match any of these (a dispel).")]
        [SerializeField] internal GameplayTagContainer removeEffectsWithTags = new();

        [SerializeField] internal EffectStackingPolicy stacking = EffectStackingPolicy.None;
        [SerializeField] internal int maxStacks = 1;
        [Tooltip("Re-applying at any stack count restarts the remaining duration.")]
        [SerializeField] internal bool refreshDurationOnStack;
        [Tooltip("Re-applying at any stack count restarts the periodic timer.")]
        [SerializeField] internal bool resetPeriodOnStack;

        public EffectDurationPolicy DurationPolicy => durationPolicy;
        public ScalableFloat Duration => duration;
        public bool IsPeriodic => isPeriodic;
        public ScalableFloat Period => period;
        public bool ExecuteOnApplication => executeOnApplication;
        public IReadOnlyList<EffectModifierDefinition> Modifiers => modifiers;
        public GameplayTagContainer GrantedTags => grantedTags;
        public GameplayTagContainer ApplicationRequiredTags => applicationRequiredTags;
        public GameplayTagContainer ApplicationBlockedTags => applicationBlockedTags;
        public GameplayTagContainer OngoingRequiredTags => ongoingRequiredTags;
        public GameplayTagContainer RemoveEffectsWithTags => removeEffectsWithTags;
        public EffectStackingPolicy Stacking => stacking;
        public int MaxStacks => maxStacks;
        public bool RefreshDurationOnStack => refreshDurationOnStack;
        public bool ResetPeriodOnStack => resetPeriodOnStack;

        private void OnValidate()
        {
            if (maxStacks < 1)
                maxStacks = 1;

            if (durationPolicy == EffectDurationPolicy.Instant)
            {
                if (isPeriodic)
                    Debug.LogWarning($"[{name}] Instant effects cannot be periodic.", this);
                if (!grantedTags.IsEmpty)
                    Debug.LogWarning($"[{name}] Instant effects grant no tags — they have no active lifetime.", this);
                if (stacking != EffectStackingPolicy.None)
                    Debug.LogWarning($"[{name}] Instant effects cannot stack — they have no active lifetime.", this);
            }
            else if (isPeriodic && period.baseValue <= 0f)
            {
                Debug.LogWarning($"[{name}] Periodic effects need a period > 0.", this);
            }
        }
    }
}
