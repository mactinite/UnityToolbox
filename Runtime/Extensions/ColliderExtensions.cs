using UnityEngine;

namespace toolbox.Extensions
{
    public static class ColliderExtensions
    {
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
    }
}
