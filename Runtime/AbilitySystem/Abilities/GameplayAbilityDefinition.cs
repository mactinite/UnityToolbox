using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// The authored half of an ability. Games extend the system through exactly one pattern:
    /// subclass this asset with the ability's data (a projectile prefab, a clip reference) plus a
    /// [CreateAssetMenu], and have <see cref="CreateSpec"/> return a matching <see cref="AbilitySpec"/>
    /// subclass that implements the behaviour. Cost and Cooldown are ordinary gameplay effects: the
    /// cost is an Instant effect that spends attributes, the cooldown a Duration effect granting a
    /// tag the ability is gated on.
    /// </summary>
    public abstract class GameplayAbilityDefinition : ScriptableObject
    {
        [Tooltip("Identity of this ability, matched by cancel/block lists (e.g. 'Ability.Attack.Heavy').")]
        [SerializeField] internal GameplayTagContainer abilityTags = new();

        [Tooltip("Activating this ability cancels active abilities whose ability tags match any of these.")]
        [SerializeField] internal GameplayTagContainer cancelAbilitiesWithTags = new();

        [Tooltip("While this ability is active, abilities whose ability tags match any of these cannot activate.")]
        [SerializeField] internal GameplayTagContainer blockAbilitiesWithTags = new();

        [Tooltip("The owner must have all of these tags to activate.")]
        [SerializeField] internal GameplayTagContainer activationRequiredTags = new();

        [Tooltip("The owner must have none of these tags to activate.")]
        [SerializeField] internal GameplayTagContainer activationBlockedTags = new();

        [Tooltip("Tags the owner has while this ability is active.")]
        [SerializeField] internal GameplayTagContainer activationOwnedTags = new();

        [Tooltip("Instant effect that pays the ability's cost. Optional; activation fails if unaffordable.")]
        [SerializeField] internal GameplayEffectDefinition cost;

        [Tooltip("Duration effect granting the cooldown tag(s). Optional; the ability is gated on those tags.")]
        [SerializeField] internal GameplayEffectDefinition cooldown;

        public GameplayTagContainer AbilityTags => abilityTags;
        public GameplayTagContainer CancelAbilitiesWithTags => cancelAbilitiesWithTags;
        public GameplayTagContainer BlockAbilitiesWithTags => blockAbilitiesWithTags;
        public GameplayTagContainer ActivationRequiredTags => activationRequiredTags;
        public GameplayTagContainer ActivationBlockedTags => activationBlockedTags;
        public GameplayTagContainer ActivationOwnedTags => activationOwnedTags;
        public GameplayEffectDefinition Cost => cost;
        public GameplayEffectDefinition Cooldown => cooldown;

        /// <summary>Creates this ability's per-owner runtime instance. Called once at grant time.</summary>
        public abstract AbilitySpec CreateSpec(AbilitySystemCore owner);
    }
}
