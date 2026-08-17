using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// The identity asset for a numeric attribute (a stat, a resource, a rating — the game decides).
    /// Values live per owner in an <see cref="AttributeSet"/>; this asset only names the attribute
    /// and optionally clamps it to a fixed numeric range.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Attribute", fileName = "Attr_New")]
    public sealed class AttributeDefinition : ScriptableObject
    {
        [Tooltip("Clamp the value to a lower bound (e.g. 0 for a resource).")]
        [SerializeField] internal bool hasMin;
        [SerializeField] internal float minValue;

        [Tooltip("Clamp the value to an upper bound.")]
        [SerializeField] internal bool hasMax;
        [SerializeField] internal float maxValue;

        public bool HasMin => hasMin;
        public float MinValue => minValue;
        public bool HasMax => hasMax;
        public float MaxValue => maxValue;

        /// <summary>Applies the optional min/max bounds to <paramref name="value"/>.</summary>
        public float Clamp(float value)
        {
            if (hasMin && value < minValue)
                value = minValue;
            if (hasMax && value > maxValue)
                value = maxValue;
            return value;
        }
    }
}
