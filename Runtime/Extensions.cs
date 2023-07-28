using System.Collections.Generic;
using UnityEngine;

namespace mactinite.ToolboxCommons
{
    public static class Extensions
    {
        public static float ForceToTorque2D(this Rigidbody2D rigidbody2D, Vector2 force, Vector2 position, ForceMode2D forceMode = ForceMode2D.Force)
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
        
        /// <summary>
        /// Transform a given vector to be relative to target transform.
        /// Eg: Use to perform movement relative to camera's view direction.
        /// </summary>

        public static Vector3 RelativeTo(this Vector3 vector3, Transform target, bool onlyLateral = true)
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

        public static Vector3 RelativeTo(this Vector3 vector3, Vector3 direction, bool onlyLateral = true)
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

        /// <summary>
        /// Loops through the array and returns the closest collider by direct distance
        /// </summary>
        /// <param name="colliders"></param>
        /// <param name="to"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        public static Collider2D GetClosestCollider2D(this Collider2D[] colliders, Vector3 to)
        {
            Collider2D closestCollider = null;
            float closestDist = float.MaxValue;
            
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;
                
                float dist = Vector2.Distance(to, colliders[i].transform.position);

                if (dist < closestDist)
                {
                    closestCollider = colliders[i];
                    closestDist = dist;
                }
            }

            return closestCollider;
        }
        
        public class DistanceComparer : IComparer<Collider2D>
        {
            private Transform _source;
 
            public DistanceComparer(Transform source)
            {
                source = source;
            }
 
            public int Compare(Collider2D a, Collider2D b)
            {
                var sourcePosition = _source.position;
                return Vector2.Distance(a.transform.position, sourcePosition).CompareTo(Vector2.Distance(b.transform.position, sourcePosition));
            }
        }
    }

}