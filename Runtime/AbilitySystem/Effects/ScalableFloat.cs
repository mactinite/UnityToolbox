using System;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// An authored float that can optionally scale with the owner's level:
    /// <c>baseValue * levelCurve.Evaluate(level)</c>. With scaling off it is just the value.
    /// </summary>
    [Serializable]
    public struct ScalableFloat
    {
        public float baseValue;
        public bool scaleWithLevel;
        public AnimationCurve levelCurve;

        public ScalableFloat(float baseValue)
        {
            this.baseValue = baseValue;
            scaleWithLevel = false;
            levelCurve = null;
        }

        public float Evaluate(float level) =>
            scaleWithLevel && levelCurve != null ? baseValue * levelCurve.Evaluate(level) : baseValue;
    }
}
