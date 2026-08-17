using toolbox.AbilitySystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>
    /// The input → ability seam: polls the keyboard and only ever talks to the ability system
    /// (movement aside). Sprint is intent-held — while Shift is down it keeps trying to activate, so
    /// sprint resumes by itself once stamina recovers past the ability's own gate.
    /// </summary>
    [RequireComponent(typeof(SampleAvatar))]
    public class SampleInputDriver : MonoBehaviour
    {
        [SerializeField] private GameplayAbilityDefinition sprintAbility;
        [SerializeField] private GameplayAbilityDefinition jumpAbility;

        private SampleAvatar avatar;

        private void Awake() => avatar = GetComponent<SampleAvatar>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var move = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            avatar.SetMoveInput(move);

            if (keyboard.spaceKey.wasPressedThisFrame)
                avatar.Abilities.TryActivateAbility(jumpAbility);

            if (keyboard.leftShiftKey.isPressed)
                avatar.Abilities.TryActivateAbility(sprintAbility);
            else
                avatar.Abilities.CancelAbility(sprintAbility);
        }
    }
}
