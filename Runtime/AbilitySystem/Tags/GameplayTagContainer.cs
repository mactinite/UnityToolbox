using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// A serializable list of tags for authoring on definitions (granted tags, requirement blocks).
    /// Runtime tag ownership lives in <see cref="CountedTagSet"/>; this is only the authored data.
    /// Null entries (empty inspector rows) are tolerated and ignored by every query.
    /// </summary>
    [Serializable]
    public sealed class GameplayTagContainer
    {
        [SerializeField] private List<GameplayTag> tags = new();

        public GameplayTagContainer()
        {
        }

        internal GameplayTagContainer(params GameplayTag[] tags)
        {
            this.tags.AddRange(tags);
        }

        /// <summary>The authored tags. May contain null entries; treat as data, not ownership.</summary>
        public IReadOnlyList<GameplayTag> Tags => tags;

        public int Count => tags.Count;

        public bool IsEmpty => tags.Count == 0;

        /// <summary>True when any authored tag is <paramref name="query"/> itself or nested under it.</summary>
        public bool ContainsMatch(GameplayTag query)
        {
            if (query == null)
                return false;

            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (tag != null && tag.Matches(query))
                    return true;
            }

            return false;
        }
    }
}
