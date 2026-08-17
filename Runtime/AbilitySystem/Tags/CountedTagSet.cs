using System;
using System.Collections.Generic;

namespace toolbox.AbilitySystem
{
    /// <summary>
    /// A reference-counted set of owned tags. Multiple sources can grant the same tag (two stun
    /// effects); the tag reads as owned until every grant is released. Owning a tag also counts as
    /// owning every ancestor, so <see cref="HasTag"/> answers hierarchy queries with one lookup.
    /// </summary>
    public sealed class CountedTagSet
    {
        private readonly Dictionary<GameplayTag, int> explicitCounts = new();
        private readonly Dictionary<GameplayTag, int> inclusiveCounts = new();

        /// <summary>Raised when a tag becomes owned, including ancestors implied by a descendant grant.</summary>
        public event Action<GameplayTag> TagAdded;

        /// <summary>Raised when a tag stops being owned, including ancestors released by a descendant removal.</summary>
        public event Action<GameplayTag> TagRemoved;

        /// <summary>Raised once per mutating call, after the individual add/remove events.</summary>
        public event Action Changed;

        /// <summary>Grant <paramref name="tag"/> <paramref name="count"/> times. Null tags are ignored.</summary>
        public void AddTag(GameplayTag tag, int count = 1)
        {
            if (tag == null || count <= 0)
                return;

            explicitCounts.TryGetValue(tag, out var explicitCount);
            explicitCounts[tag] = explicitCount + count;

            var node = tag;
            for (var depth = 0; depth < GameplayTag.MaxDepth && node != null; depth++)
            {
                inclusiveCounts.TryGetValue(node, out var inclusive);
                inclusiveCounts[node] = inclusive + count;
                if (inclusive == 0)
                    TagAdded?.Invoke(node);

                node = node.Parent;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Release up to <paramref name="count"/> grants of <paramref name="tag"/>.
        /// Returns false (and changes nothing) when the tag was not explicitly owned.
        /// </summary>
        public bool RemoveTag(GameplayTag tag, int count = 1)
        {
            if (tag == null || count <= 0 || !explicitCounts.TryGetValue(tag, out var explicitCount))
                return false;

            var removed = Math.Min(count, explicitCount);
            if (explicitCount - removed <= 0)
                explicitCounts.Remove(tag);
            else
                explicitCounts[tag] = explicitCount - removed;

            var node = tag;
            for (var depth = 0; depth < GameplayTag.MaxDepth && node != null; depth++)
            {
                inclusiveCounts.TryGetValue(node, out var inclusive);
                var remaining = inclusive - removed;
                if (remaining <= 0)
                {
                    inclusiveCounts.Remove(node);
                    TagRemoved?.Invoke(node);
                }
                else
                {
                    inclusiveCounts[node] = remaining;
                }

                node = node.Parent;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>How many times <paramref name="tag"/> itself was granted (descendants not counted).</summary>
        public int GetExplicitCount(GameplayTag tag) =>
            tag != null && explicitCounts.TryGetValue(tag, out var count) ? count : 0;

        /// <summary>True when <paramref name="tag"/> or any descendant of it is owned. O(1).</summary>
        public bool HasTag(GameplayTag tag) => tag != null && inclusiveCounts.ContainsKey(tag);

        /// <summary>
        /// True when <paramref name="tag"/> or any ANCESTOR of it was explicitly granted — the reverse
        /// question to <see cref="HasTag"/>: a grant of "Attack" covers a query for "Attack.Heavy".
        /// Used for block lists, where blocking a parent tag blocks everything under it.
        /// </summary>
        public bool HasTagOrAncestor(GameplayTag tag)
        {
            var node = tag;
            for (var depth = 0; depth < GameplayTag.MaxDepth && node != null; depth++)
            {
                if (explicitCounts.ContainsKey(node))
                    return true;

                node = node.Parent;
            }

            return false;
        }

        /// <summary>True when every tag in <paramref name="tags"/> is owned. An empty container passes.</summary>
        public bool HasAll(GameplayTagContainer tags)
        {
            if (tags == null)
                return true;

            for (var i = 0; i < tags.Tags.Count; i++)
            {
                var tag = tags.Tags[i];
                if (tag != null && !HasTag(tag))
                    return false;
            }

            return true;
        }

        /// <summary>True when at least one tag in <paramref name="tags"/> is owned. An empty container fails.</summary>
        public bool HasAny(GameplayTagContainer tags)
        {
            if (tags == null)
                return false;

            for (var i = 0; i < tags.Tags.Count; i++)
            {
                if (HasTag(tags.Tags[i]))
                    return true;
            }

            return false;
        }

        /// <summary>True when no tag in <paramref name="tags"/> is owned. An empty container passes.</summary>
        public bool HasNone(GameplayTagContainer tags) => !HasAny(tags);

        /// <summary>Copies the explicitly granted tags into <paramref name="results"/> (cleared first). Debug/UI only.</summary>
        public void GetExplicitTags(List<GameplayTag> results)
        {
            results.Clear();
            foreach (var tag in explicitCounts.Keys)
                results.Add(tag);
        }
    }
}
