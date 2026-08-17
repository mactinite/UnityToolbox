using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// All attribute values for one owner. Usable on its own (anything with numeric state can carry
    /// one — it has no dependency on abilities or effects); <see cref="AbilitySystemCore"/> owns one
    /// per character.
    /// </summary>
    public sealed class AttributeSet
    {
        private readonly Dictionary<AttributeDefinition, AttributeInstance> attributes = new();

        /// <summary>Raised with (attribute, oldCurrent, newCurrent) whenever a current value actually changes.</summary>
        public event Action<AttributeDefinition, float, float> AttributeChanged;

        /// <summary>
        /// Adds an attribute with the given base value. Adding an attribute that already exists is
        /// ignored and returns the existing instance.
        /// </summary>
        public AttributeInstance AddAttribute(AttributeDefinition definition, float baseValue)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (attributes.TryGetValue(definition, out var existing))
                return existing;

            var instance = new AttributeInstance(definition, baseValue);
            instance.changed = OnInstanceChanged;
            attributes.Add(definition, instance);
            return instance;
        }

        public bool HasAttribute(AttributeDefinition definition) =>
            definition != null && attributes.ContainsKey(definition);

        public bool TryGetAttribute(AttributeDefinition definition, out AttributeInstance instance)
        {
            instance = null;
            return definition != null && attributes.TryGetValue(definition, out instance);
        }

        /// <summary>The current (modified) value, or 0 with a warning when the attribute is absent.</summary>
        public float GetValue(AttributeDefinition definition)
        {
            if (TryGetAttribute(definition, out var instance))
                return instance.CurrentValue;

            WarnMissing(definition);
            return 0f;
        }

        /// <summary>The base (unmodified) value, or 0 with a warning when the attribute is absent.</summary>
        public float GetBaseValue(AttributeDefinition definition)
        {
            if (TryGetAttribute(definition, out var instance))
                return instance.BaseValue;

            WarnMissing(definition);
            return 0f;
        }

        public void SetBaseValue(AttributeDefinition definition, float value)
        {
            if (TryGetAttribute(definition, out var instance))
                instance.BaseValue = value;
            else
                WarnMissing(definition);
        }

        /// <summary>Adds <paramref name="delta"/> to the base value (spend/restore convenience).</summary>
        public void ModifyBaseValue(AttributeDefinition definition, float delta)
        {
            if (TryGetAttribute(definition, out var instance))
                instance.BaseValue += delta;
            else
                WarnMissing(definition);
        }

        public void AddModifier(AttributeDefinition definition, in AttributeModifier modifier)
        {
            if (TryGetAttribute(definition, out var instance))
                instance.AddModifier(in modifier);
            else
                WarnMissing(definition);
        }

        /// <summary>Removes every modifier applied with <paramref name="source"/> across all attributes.</summary>
        public bool RemoveModifiersFromSource(object source)
        {
            if (source == null)
                return false;

            var removedAny = false;
            foreach (var instance in attributes.Values)
                removedAny |= instance.RemoveModifiersFromSource(source) > 0;
            return removedAny;
        }

        private void OnInstanceChanged(AttributeInstance instance, float oldValue, float newValue) =>
            AttributeChanged?.Invoke(instance.Definition, oldValue, newValue);

        private static void WarnMissing(AttributeDefinition definition) =>
            Debug.LogWarning($"[AttributeSet] Attribute '{(definition != null ? definition.name : "<null>")}' is not present on this owner.");
    }
}
