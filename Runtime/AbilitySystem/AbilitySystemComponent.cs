using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// The MonoBehaviour host for an <see cref="AbilitySystemCore"/> — deliberately logic-free: it
    /// seeds the core from serialized authoring, ticks it every frame, and forwards the high-traffic
    /// calls. Everything else (queries, events, the tag set, the attribute set) is reached through
    /// <see cref="Core"/>, which is safe to touch from other components' Awake — it initialises on
    /// first access.
    /// </summary>
    [AddComponentMenu("Toolbox/Ability System")]
    public class AbilitySystemComponent : MonoBehaviour
    {
        /// <summary>One authored starting attribute: which, and at what base value.</summary>
        [Serializable]
        public struct AttributeStartValue
        {
            public AttributeDefinition attribute;
            public float baseValue;
        }

        [SerializeField] private float level = 1f;
        [SerializeField] private List<AttributeStartValue> initialAttributes = new();
        [SerializeField] private GameplayTagContainer defaultTags = new();
        [SerializeField] private List<GameplayAbilityDefinition> startingAbilities = new();
        [SerializeField] private List<GameplayEffectDefinition> startupEffects = new();

        private AbilitySystemCore core;
        private bool startupContentApplied;

        /// <summary>The owner's core. Initialises on first access, so pre-Awake use is safe.</summary>
        public AbilitySystemCore Core
        {
            get
            {
                EnsureInitialized();
                return core;
            }
        }

        /// <summary>Builds the core and seeds attributes and default tags. Idempotent.</summary>
        public void EnsureInitialized()
        {
            if (core != null)
                return;

            core = new AbilitySystemCore { Level = level, Context = this };

            for (var i = 0; i < initialAttributes.Count; i++)
            {
                var entry = initialAttributes[i];
                if (entry.attribute != null)
                    core.Attributes.AddAttribute(entry.attribute, entry.baseValue);
            }

            for (var i = 0; i < defaultTags.Tags.Count; i++)
                core.Tags.AddTag(defaultTags.Tags[i]);
        }

        public AbilitySpec GrantAbility(GameplayAbilityDefinition definition) => Core.GrantAbility(definition);

        public bool TryActivateAbility(GameplayAbilityDefinition definition) => Core.TryActivateAbility(definition);

        public bool CancelAbility(GameplayAbilityDefinition definition) => Core.CancelAbility(definition);

        public bool ApplyEffectToSelf(GameplayEffectDefinition definition) => Core.TryApplyEffect(definition);

        /// <summary>Applies an effect authored by this owner to another owner (this side captures Source attributes).</summary>
        public bool ApplyEffectToTarget(GameplayEffectDefinition definition, AbilitySystemComponent target, float level = -1f) =>
            target != null && target.Core.TryApplyEffect(Core.MakeEffectSpec(definition, level), out _);

        public float GetAttributeValue(AttributeDefinition attribute) => Core.Attributes.GetValue(attribute);

        public float GetAttributeBaseValue(AttributeDefinition attribute) => Core.Attributes.GetBaseValue(attribute);

        public void SetAttributeBaseValue(AttributeDefinition attribute, float value) => Core.Attributes.SetBaseValue(attribute, value);

        public void ModifyAttributeBaseValue(AttributeDefinition attribute, float delta) => Core.Attributes.ModifyBaseValue(attribute, delta);

        public bool HasTag(GameplayTag tag) => Core.Tags.HasTag(tag);

        public void AddLooseTag(GameplayTag tag) => Core.Tags.AddTag(tag);

        public void RemoveLooseTag(GameplayTag tag) => Core.Tags.RemoveTag(tag);

        private void Awake() => EnsureInitialized();

        // Granting waits for Start so every component's Awake can subscribe to core events first.
        private void Start()
        {
            if (startupContentApplied)
                return;

            startupContentApplied = true;
            for (var i = 0; i < startingAbilities.Count; i++)
            {
                if (startingAbilities[i] != null)
                    core.GrantAbility(startingAbilities[i]);
            }

            for (var i = 0; i < startupEffects.Count; i++)
            {
                if (startupEffects[i] != null)
                    core.TryApplyEffect(startupEffects[i]);
            }
        }

        private void Update() => core.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            // Abilities may hold scene references; make sure they wind down with the owner.
            core?.CancelAllAbilities();
        }
    }
}
