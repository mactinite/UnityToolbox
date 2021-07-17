using UnityEngine;

namespace mactinite.ToolboxCommons
{
    public static class Extensions
    {
        public static float ForceToTorque(this Rigidbody2D rigidbody2D, Vector2 force, Vector2 position, ForceMode2D forceMode = ForceMode2D.Force)
        {
            // Vector from the force position to the CoM
            Vector2 p = rigidbody2D.worldCenterOfMass - position;

            // Get the angle between the force and the vector from position to CoM
            float angle = Mathf.Atan2(p.y, p.x) - Mathf.Atan2(force.y, force.x);

            // This is basically like Vector3.Cross, but in 2D, hence giving just a scalar value instead of a Vector3
            float t = p.magnitude * force.magnitude * Mathf.Sin(angle) * Mathf.Rad2Deg;

            // Continuous force
            if (forceMode == ForceMode2D.Force) t *= Time.fixedDeltaTime;

            // Apply inertia
            return t / rigidbody2D.inertia;
        }

        /// <summary>
        /// Checks if a GameObject is in a LayerMask
        /// </summary>
        /// <param name="obj">GameObject to test</param>
        /// <param name="layerMask">LayerMask with all the layers to test against</param>
        /// <returns>True if in any of the layers in the LayerMask</returns>
        public static bool IsInLayerMask(this GameObject obj, LayerMask layerMask)
        {
            // Convert the object's layer to a bitfield for comparison
            int objLayerMask = (1 << obj.layer);
            if ((layerMask.value & objLayerMask) > 0)  // Extra round brackets required!
                return true;
            else
                return false;
        }
    }

}