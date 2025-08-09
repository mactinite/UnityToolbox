using System;
using UnityEngine;

namespace toolbox.Extensions
{
    public static class RigidbodyExtensions
    {
        public static float ForceToTorque2D(
            this Rigidbody2D rigidbody2D,
            Vector2 force,
            Vector2 position,
            ForceMode2D forceMode = ForceMode2D.Force
        )
        {
            // Vector from the force position to the CoM
            Vector2 p = rigidbody2D.worldCenterOfMass - position;

            // Get the angle between the force and the vector from position to CoM
            float angle = Mathf.Atan2(p.y, p.x) - Mathf.Atan2(force.y, force.x);

            // This is basically like Vector3.Cross, but in 2D, hence giving just a scalar value instead of a Vector3
            float t = p.magnitude * force.magnitude * Mathf.Sin(angle) * Mathf.Rad2Deg;

            // Continuous force
            if (forceMode == ForceMode2D.Force)
                t *= Time.fixedDeltaTime;

            // Apply inertia
            return t / rigidbody2D.inertia;
        }
    }
}
