using System;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>One authored modifier row on a gameplay effect: which attribute, how, by how much.</summary>
    [Serializable]
    public struct EffectModifierDefinition
    {
        public AttributeDefinition attribute;
        public ModifierChannel channel;
        public ScalableFloat magnitude;

        [Tooltip("Optional calculation asset; replaces the inline magnitude when set.")]
        public MagnitudeCalculation customMagnitude;
    }
}
