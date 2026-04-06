using UnityEngine;

namespace toolbox.Extensions
{
    public static class TransformExtensions
    {
        public static void DestroyChildren(this Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Recursively searches for a child transform by name.
        /// </summary>
        /// <param name="parent">The transform to search from.</param>
        /// <param name="childName">The name of the child to find.</param>
        /// <returns>The found transform, or null if not found.</returns>
        public static Transform FindDeepChild(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                var result = child.FindDeepChild(childName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}