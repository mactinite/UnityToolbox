using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>
    /// Held sprint: while active it applies two effects — a speed buff granting the sprint tag, and a
    /// periodic stamina drain — and ends itself when stamina runs dry. Demonstrates the definition +
    /// spec extension pattern, continuous attribute modifiers, periodic effects, and a custom
    /// activation gate.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Samples/Sprint Ability", fileName = "Ability_Sprint")]
    public sealed class SprintAbilityDefinition : GameplayAbilityDefinition
    {
        [Tooltip("Infinite effect applied while sprinting: the speed modifier + sprint tag.")]
        public GameplayEffectDefinition sprintingEffect;

        [Tooltip("Infinite periodic effect applied while sprinting: the stamina drain.")]
        public GameplayEffectDefinition drainEffect;

        public AttributeDefinition staminaAttribute;

        [Tooltip("Minimum stamina to start sprinting, so it can't restart-stutter at 0.")]
        public float minimumStaminaToStart = 10f;

        public override AbilitySpec CreateSpec(AbilitySystemCore owner) => new SprintAbilitySpec(this, owner);
    }

    public sealed class SprintAbilitySpec : AbilitySpec
    {
        private readonly SprintAbilityDefinition data;

        // Effect handles are per-spec state; shared definition assets must stay stateless.
        private ActiveGameplayEffect sprinting;
        private ActiveGameplayEffect drain;

        public SprintAbilitySpec(SprintAbilityDefinition definition, AbilitySystemCore owner)
            : base(definition, owner)
        {
            data = definition;
        }

        public override bool CanActivate() =>
            base.CanActivate()
            && Owner.Attributes.GetValue(data.staminaAttribute) >= data.minimumStaminaToStart;

        protected override void OnActivate()
        {
            Owner.TryApplyEffect(Owner.MakeEffectSpec(data.sprintingEffect, Level, instigator: this), out sprinting);
            Owner.TryApplyEffect(Owner.MakeEffectSpec(data.drainEffect, Level, instigator: this), out drain);
        }

        protected override void OnTick(float deltaTime)
        {
            if (Owner.Attributes.GetValue(data.staminaAttribute) <= 0f)
                End();
        }

        protected override void OnEnd(bool cancelled)
        {
            Owner.RemoveEffect(sprinting);
            Owner.RemoveEffect(drain);
            sprinting = null;
            drain = null;
        }
    }
}
