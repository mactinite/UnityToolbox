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
        
    }
}