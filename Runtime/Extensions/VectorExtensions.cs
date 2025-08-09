using UnityEngine;

namespace toolbox.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Transform a given vector to be relative to target transform.
        /// Eg: Use to perform movement relative to camera's view direction.
        /// </summary>
        public static Vector3 RelativeTo(
            this Vector3 vector3,
            Transform target,
            bool onlyLateral = true
        )
        {
            var forward = target.forward;

            if (onlyLateral)
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);

            return Quaternion.LookRotation(forward) * vector3;
        }

        /// <summary>
        /// Transform a given vector to be relative to target transform.
        /// Eg: Use to perform movement relative to camera's view direction.
        /// </summary>
        public static Vector3 RelativeTo(
            this Vector3 vector3,
            Vector3 direction,
            bool onlyLateral = true
        )
        {
            var forward = direction;

            if (onlyLateral)
                forward = Vector3.ProjectOnPlane(forward, Vector3.up);

            return Quaternion.LookRotation(forward) * vector3;
        }

        /// <summary>
        /// Transform a given vector to be relative to target transform.
        /// Eg: Use to perform movement relative to camera's view direction.
        /// </summary>
        public static Vector2 RelativeTo(this Vector2 vector2, Vector2 direction)
        {
            var up = direction;

            return Quaternion.LookRotation(Vector3.forward, up) * vector2;
        }
    }
}
