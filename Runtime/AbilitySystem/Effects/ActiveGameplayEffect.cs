using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// Bookkeeping for one Duration/Infinite effect on its target: remaining lifetime, stack count,
    /// the periodic timer, and inhibition state. This instance is also the source key its attribute
    /// modifiers are applied under, so removing the effect removes exactly its modifiers.
    /// </summary>
    public sealed class ActiveGameplayEffect
    {
        /// <summary>Continuous-modifier magnitudes snapshot at application (one per modifier row, per stack).</summary>
        internal float[] snapshotMagnitudes;

        internal float timeUntilPeriodTick;

        /// <summary>Set on removal so re-entrant callbacks and the tick loop can skip this instance.</summary>
        internal bool isRemoved;

        internal ActiveGameplayEffect(GameplayEffectSpec spec)
        {
            Spec = spec;
            TotalDuration = spec.Definition.DurationPolicy == EffectDurationPolicy.Duration
                ? spec.EvaluateDuration()
                : float.PositiveInfinity;
            RemainingDuration = TotalDuration;
            timeUntilPeriodTick = spec.EvaluatePeriod();
        }

        public GameplayEffectSpec Spec { get; }

        /// <summary>Seconds left, or positive infinity for Infinite effects.</summary>
        public float RemainingDuration { get; internal set; }

        public float TotalDuration { get; internal set; }

        public int StackCount { get; internal set; } = 1;

        /// <summary>True while the effect's ongoing tag requirements are unmet: modifiers, granted tags
        /// and periodic executions are suspended, but the duration keeps ticking.</summary>
        public bool IsInhibited { get; internal set; }

        internal void Tick(float deltaTime, out bool expired, out int periodicExecutions)
        {
            periodicExecutions = 0;

            if (!float.IsPositiveInfinity(RemainingDuration))
                RemainingDuration = Mathf.Max(0f, RemainingDuration - deltaTime);
            expired = RemainingDuration <= 0f && !float.IsPositiveInfinity(TotalDuration);

            var definition = Spec.Definition;
            if (!definition.IsPeriodic || IsInhibited)
                return;

            var period = Spec.EvaluatePeriod();
            if (period <= 0f)
                return;

            timeUntilPeriodTick -= deltaTime;
            while (timeUntilPeriodTick <= 0f)
            {
                periodicExecutions++;
                timeUntilPeriodTick += period;
            }
        }
    }
}
