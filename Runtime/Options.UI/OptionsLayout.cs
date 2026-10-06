using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace toolbox.Options.UI
{
    /// <summary>
    /// The arrangement of a settings menu: pages, each with sections, each with entries. Build it in code with the
    /// fluent helpers, convert an <see cref="OptionsLayoutAsset"/>, or let <see cref="FromStore"/> make one page per
    /// category. The layout only names options; the store supplies them when the menu is built.
    /// </summary>
    public sealed class OptionsLayout
    {
        public List<OptionsPage> Pages { get; } = new List<OptionsPage>();

        public OptionsPage Page(string id, string label = null)
        {
            var page = new OptionsPage(id, label);
            Pages.Add(page);
            return page;
        }

        /// <summary>One page per category in registration order, options by <see cref="Option.Order"/>.</summary>
        public static OptionsLayout FromStore(OptionsStore store)
        {
            var layout = new OptionsLayout();
            foreach (var category in store.Categories)
            {
                var page = layout.Page(category, category);
                foreach (var option in store.InCategory(category))
                    page.Option(option.Id);
            }

            return layout;
        }

        public IEnumerable<OptionEntry> OptionEntries => Pages.SelectMany(page => page.OptionEntries);

        /// <summary>Option ids the layout names that the store does not have.</summary>
        public IEnumerable<string> UnknownIds(OptionsStore store) =>
            OptionEntries.Select(entry => entry.OptionId).Where(id => !store.Contains(id)).Distinct();

        /// <summary>Ids placed more than once.</summary>
        public IEnumerable<string> DuplicateIds() =>
            OptionEntries.GroupBy(entry => entry.OptionId).Where(group => group.Count() > 1).Select(group => group.Key);

        /// <summary>Registered, visible options the layout does not place anywhere.</summary>
        public IEnumerable<Option> Unplaced(OptionsStore store)
        {
            var placed = new HashSet<string>(OptionEntries.Select(entry => entry.OptionId));
            return store.All.Where(option => !option.HasFlag(OptionFlags.Hidden) && !placed.Contains(option.Id));
        }
    }

    public sealed class OptionsPage
    {
        OptionsSection current;

        public OptionsPage(string id, string label = null)
        {
            Id = id;
            Label = label;
        }

        public string Id { get; }
        public string Label { get; set; }
        public List<OptionsSection> Sections { get; } = new List<OptionsSection>();

        public IEnumerable<OptionEntry> OptionEntries => Sections.SelectMany(section => section.Entries).OfType<OptionEntry>();

        /// <summary>Starts a new section; a label becomes a header row.</summary>
        public OptionsPage Section(string label = null)
        {
            current = new OptionsSection(label);
            Sections.Add(current);
            return this;
        }

        public OptionsPage Option(string optionId, Action<OptionEntry> configure = null)
        {
            var entry = new OptionEntry(optionId);
            configure?.Invoke(entry);
            CurrentSection().Entries.Add(entry);
            return this;
        }

        public OptionsPage Options(params string[] optionIds)
        {
            foreach (var id in optionIds)
                Option(id);
            return this;
        }

        public OptionsPage Header(string text)
        {
            CurrentSection().Entries.Add(new HeaderEntry(text));
            return this;
        }

        public OptionsPage Spacer(float height = 12f)
        {
            CurrentSection().Entries.Add(new SpacerEntry(height));
            return this;
        }

        /// <summary>Any prefab (a credits button, a link row); <paramref name="configure"/> runs on the instance.</summary>
        public OptionsPage Custom(GameObject prefab, Action<GameObject> configure = null)
        {
            CurrentSection().Entries.Add(new CustomEntry(prefab, configure));
            return this;
        }

        OptionsSection CurrentSection()
        {
            if (current == null)
            {
                current = new OptionsSection(null);
                Sections.Add(current);
            }

            return current;
        }
    }

    public sealed class OptionsSection
    {
        public OptionsSection(string label)
        {
            Label = label;
        }

        /// <summary>Header text, or null for no header.</summary>
        public string Label { get; set; }
        public List<OptionsEntry> Entries { get; } = new List<OptionsEntry>();
    }

    public abstract class OptionsEntry
    {
    }

    /// <summary>An option by id, with optional per-placement overrides.</summary>
    public sealed class OptionEntry : OptionsEntry
    {
        public OptionEntry(string optionId)
        {
            OptionId = optionId;
        }

        public string OptionId { get; }
        public string Label { get; set; }
        public string Description { get; set; }
        public OptionPresentation? Presentation { get; set; }
        /// <summary>A row prefab for this placement only. Must carry an <see cref="OptionRow"/>.</summary>
        public GameObject RowPrefab { get; set; }
    }

    public sealed class HeaderEntry : OptionsEntry
    {
        public HeaderEntry(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
    }

    public sealed class SpacerEntry : OptionsEntry
    {
        public SpacerEntry(float height)
        {
            Height = height;
        }

        public float Height { get; set; }
    }

    public sealed class CustomEntry : OptionsEntry
    {
        public CustomEntry(GameObject prefab, Action<GameObject> configure = null)
        {
            Prefab = prefab;
            Configure = configure;
        }

        public GameObject Prefab { get; }
        public Action<GameObject> Configure { get; }
    }
}
