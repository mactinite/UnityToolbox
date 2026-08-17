using toolbox.AbilitySystem;
using UnityEngine;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>
    /// A minimal capsule mover for the demo. The one ability-system touchpoint: its planar speed is
    /// read from the MoveSpeed attribute every frame, so effects that modify the attribute (the
    /// sprint buff) change movement without this class knowing about them.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(AbilitySystemComponent))]
    public class SampleAvatar : MonoBehaviour
    {
        [SerializeField] private AttributeDefinition moveSpeedAttribute;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float turnSpeed = 12f;

        private CharacterController controller;
        private AbilitySystemComponent abilities;
        private Vector2 moveInput;
        private float verticalVelocity;

        public AbilitySystemComponent Abilities => abilities;

        public bool IsGrounded => controller.isGrounded;

        /// <summary>Planar speed for the HUD.</summary>
        public float CurrentSpeed
        {
            get
            {
                var velocity = controller.velocity;
                velocity.y = 0f;
                return velocity.magnitude;
            }
        }

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);

        /// <summary>Launches the jump; the jump ability calls this after cost and cooldown commit.</summary>
        public void Jump() => verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            abilities = GetComponent<AbilitySystemComponent>();
        }

        private void Update()
        {
            var speed = abilities.GetAttributeValue(moveSpeedAttribute);
            var planar = new Vector3(moveInput.x, 0f, moveInput.y) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity -= gravity * Time.deltaTime;

            controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (planar.sqrMagnitude > 0.01f)
            {
                var look = Quaternion.LookRotation(planar);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }
    }
}
