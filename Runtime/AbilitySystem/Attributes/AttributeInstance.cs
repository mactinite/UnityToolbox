using System;
using System.Collections.Generic;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// The live value of one attribute on one owner: a base value plus applied modifiers, with the
    /// current value cached and recomputed eagerly on every mutation (there is no per-frame work).
    /// Formula: <c>(Base + ΣAdd) * (1 + ΣPercentAdd) * Π(1 + PercentMult)</c>, unless an Override
    /// modifier is present — then the most recently applied Override wins outright.
    /// </summary>
    public sealed class AttributeInstance
    {
        private readonly List<AttributeModifier> modifiers = new();
        private float baseValue;
        private float currentValue;

        /// <summary>Set by the owning <see cref="AttributeSet"/>; receives (instance, oldCurrent, newCurrent).</summary>
        internal Action<AttributeInstance, float, float> changed;

        internal AttributeInstance(AttributeDefinition definition, float baseValue)
        {
            Definition = definition;
            this.baseValue = definition.Clamp(baseValue);
            currentValue = this.baseValue;
        }

        public AttributeDefinition Definition { get; }

        /// <summary>The computed value after modifiers and clamping.</summary>
        public float CurrentValue => currentValue;

        /// <summary>The persistent value instant effects and direct writes act on. Setting it is clamped.</summary>
        public float BaseValue
        {
            get => baseValue;
            set
            {
                baseValue = Definition.Clamp(value);
                Recompute();
            }
        }

        internal void AddModifier(in AttributeModifier modifier)
        {
            modifiers.Add(modifier);
            Recompute();
        }

        internal int RemoveModifiersFromSource(object source)
        {
            var removed = 0;
            for (var i = modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(modifiers[i].Source, source))
                {
                    modifiers.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
                Recompute();
            return removed;
        }

        private void Recompute()
        {
            var old = currentValue;
            currentValue = Definition.Clamp(Calculate());
            if (old != currentValue)
                changed?.Invoke(this, old, currentValue);
        }

        private float Calculate()
        {
            for (var i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].Channel == ModifierChannel.Override)
                    return modifiers[i].Value;
            }

            var add = 0f;
            var percentAdd = 0f;
            var percentMult = 1f;
            for (var i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                switch (modifier.Channel)
                {
                    case ModifierChannel.Add:
                        add += modifier.Value;
                        break;
                    case ModifierChannel.PercentAdd:
                        percentAdd += modifier.Value;
                        break;
                    case ModifierChannel.PercentMult:
                        percentMult *= 1f + modifier.Value;
                        break;
                }
            }

            return (baseValue + add) * (1f + percentAdd) * percentMult;
        }
    }
}
