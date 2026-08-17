namespace toolbox.AbilitySystem
{
    /// <summary>How a modifier combines with an attribute's base value.</summary>
    public enum ModifierChannel
    {
        /// <summary>Added to the base value before percentages apply.</summary>
        Add,

        /// <summary>Summed with other PercentAdd modifiers into one multiplier: +10% and +20% give ×1.3.</summary>
        PercentAdd,

        /// <summary>Multiplied with other PercentMult modifiers: +10% and +20% give ×1.32.</summary>
        PercentMult,

        /// <summary>Replaces the computed value entirely; the most recently applied Override wins.</summary>
        Override,
    }

    /// <summary>
    /// A single applied modifier. <see cref="Source"/> is the removal key: every application must use
    /// a unique live object (an active effect, an equip record), never a shared asset, so one source's
    /// modifiers can be removed in bulk without disturbing anyone else's.
    /// </summary>
    public readonly struct AttributeModifier
    {
        public readonly ModifierChannel Channel;
        public readonly float Value;
        public readonly object Source;

        public AttributeModifier(ModifierChannel channel, float value, object source)
        {
            Channel = channel;
            Value = value;
            Source = source;
        }
    }
}
