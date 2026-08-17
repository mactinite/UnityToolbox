using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>
    /// Instant jump: pays its stamina cost, starts its cooldown (both authored as ordinary gameplay
    /// effects on the definition asset), launches the avatar and ends inside OnActivate. Demonstrates
    /// cost + cooldown commit and a scene-facing activation gate (grounded).
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Samples/Jump Ability", fileName = "Ability_Jump")]
    public sealed class JumpAbilityDefinition : GameplayAbilityDefinition
    {
        public override AbilitySpec CreateSpec(AbilitySystemCore owner) => new JumpAbilitySpec(this, owner);
    }

    public sealed class JumpAbilitySpec : AbilitySpec
    {
        private SampleAvatar avatar;

        public JumpAbilitySpec(JumpAbilityDefinition definition, AbilitySystemCore owner)
            : base(definition, owner)
        {
        }

        private SampleAvatar Avatar
        {
            get
            {
                if (avatar == null && Owner.Context is AbilitySystemComponent host)
                    avatar = host.GetComponent<SampleAvatar>();
                return avatar;
            }
        }

        public override bool CanActivate() => base.CanActivate() && Avatar != null && Avatar.IsGrounded;

        protected override void OnActivate()
        {
            Avatar.Jump();
            End();
        }
    }
}
