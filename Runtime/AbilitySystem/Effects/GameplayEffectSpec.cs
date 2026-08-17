using System.Collections.Generic;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// A runtime instance of a <see cref="GameplayEffectDefinition"/>, created by the source owner and
    /// carrying everything context-dependent: level, instigator, and attribute values snapshot at
    /// application. Create one via <see cref="AbilitySystemCore.MakeEffectSpec"/>.
    /// </summary>
    public sealed class GameplayEffectSpec
    {
        private Dictionary<(AttributeDefinition, CaptureSource), float> capturedAttributes;

        internal GameplayEffectSpec(GameplayEffectDefinition definition, AbilitySystemCore source, float level)
        {
            Definition = definition;
            Source = source;
            Level = level;
        }

        public GameplayEffectDefinition Definition { get; }

        /// <summary>The owner that created this spec. May be the same as <see cref="Target"/>.</summary>
        public AbilitySystemCore Source { get; }

        /// <summary>The owner this spec was applied to; set during application.</summary>
        public AbilitySystemCore Target { get; internal set; }

        public float Level { get; set; }

        /// <summary>Opaque application context (the ability, the weapon, the projectile — the game decides).</summary>
        public object Instigator { get; set; }

        /// <summary>Snapshots the attribute's current value for later <see cref="ResolveAttributeValue"/> reads.</summary>
        public void CaptureAttribute(AttributeDefinition attribute, CaptureSource from)
        {
            if (attribute == null)
                return;

            capturedAttributes ??= new Dictionary<(AttributeDefinition, CaptureSource), float>();
            capturedAttributes[(attribute, from)] = ReadLiveValue(attribute, from);
        }

        public bool TryGetCapturedAttribute(AttributeDefinition attribute, CaptureSource from, out float value)
        {
            value = 0f;
            return capturedAttributes != null && capturedAttributes.TryGetValue((attribute, from), out value);
        }

        /// <summary>The captured value when one exists and is preferred, otherwise the live attribute value.</summary>
        public float ResolveAttributeValue(AttributeDefinition attribute, CaptureSource from, bool preferCaptured)
        {
            if (preferCaptured && TryGetCapturedAttribute(attribute, from, out var captured))
                return captured;

            return ReadLiveValue(attribute, from);
        }

        internal float EvaluateDuration() => Definition.Duration.Evaluate(Level);

        internal float EvaluatePeriod() => Definition.Period.Evaluate(Level);

        internal float EvaluateModifierMagnitude(int modifierIndex)
        {
            var modifier = Definition.Modifiers[modifierIndex];
            return modifier.customMagnitude != null
                ? modifier.customMagnitude.Evaluate(this)
                : modifier.magnitude.Evaluate(Level);
        }

        /// <summary>Runs each custom magnitude's snapshot hook. Called once, after <see cref="Target"/> is set.</summary>
        internal void RunOnApplicationCaptures()
        {
            var modifiers = Definition.Modifiers;
            for (var i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].customMagnitude != null)
                    modifiers[i].customMagnitude.OnApplication(this);
            }
        }

        private float ReadLiveValue(AttributeDefinition attribute, CaptureSource from)
        {
            var owner = from == CaptureSource.Source ? Source : Target;
            if (owner == null || !owner.Attributes.TryGetAttribute(attribute, out var instance))
                return 0f;

            return instance.CurrentValue;
        }
    }
}
