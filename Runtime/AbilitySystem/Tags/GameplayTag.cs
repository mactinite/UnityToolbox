using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// An identity asset describing a piece of gameplay state ("Status.Stunned"), an ability
    /// identity, or an effect interaction. Tags form a hierarchy through <see cref="Parent"/>:
    /// a query for an ancestor ("Status") matches any owned descendant ("Status.Stunned").
    /// Tags are compared by asset identity, so renaming an asset never breaks references.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Ability System/Gameplay Tag", fileName = "Tag_New")]
    public sealed class GameplayTag : ScriptableObject
    {
        /// <summary>Cap on ancestor walks; keeps traversal finite if a parent cycle is authored.</summary>
        internal const int MaxDepth = 16;

        [Tooltip("Optional parent tag: 'Status.Stunned' points at 'Status'. Leave empty for a root tag.")]
        [SerializeField] internal GameplayTag parent;

        /// <summary>The tag this tag is nested under, or null for a root tag.</summary>
        public GameplayTag Parent => parent;

        /// <summary>True when <paramref name="ancestor"/> appears anywhere above this tag (strict — a tag is not its own ancestor).</summary>
        public bool IsDescendantOf(GameplayTag ancestor)
        {
            if (ancestor == null)
                return false;

            var tag = parent;
            for (var depth = 0; depth < MaxDepth && tag != null; depth++)
            {
                if (tag == ancestor)
                    return true;

                tag = tag.parent;
            }

            return false;
        }

        /// <summary>True when this tag is <paramref name="query"/> itself or nested anywhere under it.</summary>
        public bool Matches(GameplayTag query) => query == this || IsDescendantOf(query);

        /// <summary>Dot-separated path from the root ancestor to this tag, e.g. "Status.Stunned". Display/debug only.</summary>
        public string GetFullPath()
        {
            if (parent == null)
                return name;

            var path = name;
            var tag = parent;
            for (var depth = 0; depth < MaxDepth && tag != null; depth++)
            {
                path = tag.name + "." + path;
                tag = tag.parent;
            }

            return path;
        }
    }
}
