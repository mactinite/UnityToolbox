using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.Options.UI
{
    /// <summary>
    /// An <see cref="OptionsLayout"/> authored in the inspector: pages, sections and entries, with option ids picked
    /// from the catalogues. <see cref="ToLayout"/> converts it when the menu opens.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Options/Layout", fileName = "OptionsLayout")]
    public sealed class OptionsLayoutAsset : ScriptableObject
    {
        public enum EntryKind
        {
            Option,
            Header,
            Spacer,
            Custom,
        }

        [Serializable]
        public sealed class Entry
        {
            public EntryKind kind = EntryKind.Option;
            [OptionId] public string optionId;
            [Tooltip("Header text, or a label override for an option.")]
            public string label;
            [TextArea] public string description;
            public bool overridePresentation;
            public OptionPresentation presentation;
            [Tooltip("Row prefab for an option, or the prefab of a custom entry.")]
            public GameObject prefab;
            public float height = 12f;
        }

        [Serializable]
        public sealed class Section
        {
            [Tooltip("Shown as a header; leave empty for none.")]
            public string label;
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        public sealed class Page
        {
            public string id;
            public string label;
            public List<Section> sections = new List<Section>();
        }

        public List<Page> pages = new List<Page>();

        public OptionsLayout ToLayout()
        {
            var layout = new OptionsLayout();
            foreach (var pageData in pages)
            {
                var page = layout.Page(pageData.id, string.IsNullOrEmpty(pageData.label) ? pageData.id : pageData.label);
                foreach (var sectionData in pageData.sections)
                {
                    page.Section(string.IsNullOrEmpty(sectionData.label) ? null : sectionData.label);
                    foreach (var entry in sectionData.entries)
                    {
                        switch (entry.kind)
                        {
                            case EntryKind.Option:
                                page.Option(entry.optionId, placed =>
                                {
                                    placed.Label = string.IsNullOrEmpty(entry.label) ? null : entry.label;
                                    placed.Description = string.IsNullOrEmpty(entry.description) ? null : entry.description;
                                    placed.Presentation = entry.overridePresentation ? entry.presentation : (OptionPresentation?)null;
                                    placed.RowPrefab = entry.prefab;
                                });
                                break;
                            case EntryKind.Header:
                                page.Header(entry.label);
                                break;
                            case EntryKind.Spacer:
                                page.Spacer(entry.height);
                                break;
                            case EntryKind.Custom:
                                page.Custom(entry.prefab);
                                break;
                        }
                    }
                }
            }

            return layout;
        }

        /// <summary>Problems a menu built from this asset would have against <paramref name="store"/>.</summary>
        public List<string> Validate(OptionsStore store)
        {
            var issues = new List<string>();
            var layout = ToLayout();
            foreach (var id in layout.UnknownIds(store))
                issues.Add($"Unknown option id '{id}'.");
            foreach (var id in layout.DuplicateIds())
                issues.Add($"Option '{id}' is placed more than once.");
            foreach (var option in layout.Unplaced(store))
                issues.Add($"Option '{option.Id}' is registered but not placed.");
            for (int i = 0; i < pages.Count; i++)
            {
                if (string.IsNullOrEmpty(pages[i].id))
                    issues.Add($"Page {i} has no id.");
            }

            return issues;
        }
    }
}
