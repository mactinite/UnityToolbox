using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>Whose attributes a magnitude reads from.</summary>
    public enum CaptureSource
    {
        /// <summary>The owner that created the effect spec.</summary>
        Source,

        /// <summary>The owner the effect is applied to.</summary>
        Target,
    }

    /// <summary>When an attribute-backed magnitude reads its value.</summary>
    public enum CaptureTiming
    {
        /// <summary>Snapshot once when the effect is applied; later attribute changes don't affect it.</summary>
        OnApplication,

        /// <summary>Read live at each evaluation. Only meaningful for instant and periodic executions —
        /// non-periodic durational modifiers are snapshot at application regardless.</summary>
        OnEvaluation,
    }

    /// <summary>
    /// Extension point for computed effect magnitudes ("damage = 0.5 × source Strength").
    /// Assign one to a modifier row to replace its inline value. Implementations must be stateless —
    /// per-application data belongs on the <see cref="GameplayEffectSpec"/>.
    /// </summary>
    public abstract class MagnitudeCalculation : ScriptableObject
    {
        /// <summary>Called once when the spec is applied to a target — the snapshot window.</summary>
        public virtual void OnApplication(GameplayEffectSpec spec)
        {
        }

        /// <summary>The magnitude for this spec at evaluation time.</summary>
        public abstract float Evaluate(GameplayEffectSpec spec);
    }
}
