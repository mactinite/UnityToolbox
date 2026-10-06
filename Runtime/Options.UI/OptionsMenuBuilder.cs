using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>
    /// Turns a page of an <see cref="OptionsLayout"/> into rows under a transform, resolving each row's prefab
    /// through the theme, and wires explicit up/down navigation. Host-agnostic: <see cref="OptionsMenu"/> uses it,
    /// and a game can drive it from its own screen.
    /// </summary>
    public sealed class OptionsMenuBuilder
    {
        public OptionsMenuBuilder(OptionsMenuContext context, OptionsTheme theme = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Theme = theme;
        }

        public OptionsMenuContext Context { get; }
        public OptionsTheme Theme { get; set; }

        /// <summary>A row was created and bound. Decorate it here (tooltips, animation, sounds).</summary>
        public event Action<OptionRow, OptionEntry> RowCreated;

        /// <summary>A header, spacer or custom element was created.</summary>
        public event Action<GameObject, OptionsEntry> ElementCreated;

        /// <summary>A section header was created.</summary>
        public event Action<GameObject, OptionsSection> SectionHeaderCreated;

        /// <summary>Builds the page's rows and elements under <paramref name="content"/> and returns the rows in order.</summary>
        public List<OptionRow> BuildPage(OptionsPage page, Transform content)
        {
            var rows = new List<OptionRow>();
            foreach (var section in page.Sections)
            {
                if (!string.IsNullOrEmpty(section.Label))
                {
                    var header = SpawnHeader(content, Context.Text.SectionLabel(section));
                    SectionHeaderCreated?.Invoke(header, section);
                }

                foreach (var entry in section.Entries)
                {
                    switch (entry)
                    {
                        case OptionEntry optionEntry:
                            var row = BuildRow(optionEntry, content);
                            if (row != null)
                                rows.Add(row);
                            break;
                        case HeaderEntry headerEntry:
                            ElementCreated?.Invoke(SpawnHeader(content, headerEntry.Text), headerEntry);
                            break;
                        case SpacerEntry spacerEntry:
                            ElementCreated?.Invoke(SpawnSpacer(content, spacerEntry.Height), spacerEntry);
                            break;
                        case CustomEntry customEntry:
                            if (customEntry.Prefab == null)
                                break;
                            var custom = UnityEngine.Object.Instantiate(customEntry.Prefab, content);
                            customEntry.Configure?.Invoke(custom);
                            ElementCreated?.Invoke(custom, customEntry);
                            break;
                    }
                }
            }

            WireNavigation(rows);
            return rows;
        }

        /// <summary>Builds one row, or null when the option is unknown, hidden, dev-only in a release build, or unavailable.</summary>
        public OptionRow BuildRow(OptionEntry entry, Transform content)
        {
            var option = Context.Store.Get(entry.OptionId);
            if (option == null)
            {
                Debug.LogWarning($"[Options] The layout names an option '{entry.OptionId}' that is not registered.");
                return null;
            }

            if (!option.IsAvailable || option.HasFlag(OptionFlags.Hidden))
                return null;
            if (option.HasFlag(OptionFlags.DevOnly) && !Context.ShowDevOnly())
                return null;

            var prefab = ResolveRowPrefab(option, entry, out var kind);
            var instance = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, content)
                : DefaultOptionsUI.CreateRow(kind, content);
            instance.name = option.Id;

            var row = instance.GetComponent<OptionRow>();
            if (row == null)
            {
                Debug.LogError($"[Options] The row prefab for '{option.Id}' has no OptionRow component.", prefab);
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            row.Bind(option, entry, Context);
            row.gameObject.SetActive(Context.ShouldShow(option));
            RowCreated?.Invoke(row, entry);
            return row;
        }

        /// <summary>
        /// The prefab for an option, first hit wins: the entry's prefab, the theme's per-id override, the theme's
        /// custom-presentation prefab, the theme's prefab for the row kind. Null means the built-in row.
        /// </summary>
        public GameObject ResolveRowPrefab(Option option, OptionEntry entry, out RowKind kind)
        {
            kind = KindFor(option, entry);
            if (entry?.RowPrefab != null)
                return entry.RowPrefab;
            if (Theme == null)
                return null;

            var byId = Theme.RowOverrideFor(option.Id);
            if (byId != null)
                return byId;

            var presentation = entry?.Presentation ?? option.Presentation;
            if (presentation == OptionPresentation.Custom)
            {
                var custom = Theme.CustomPresentationFor(option.CustomPresentation);
                if (custom != null)
                    return custom;
            }

            return Theme.RowPrefabFor(kind);
        }

        /// <summary>The row kind for an option: its presentation hint, else by type.</summary>
        public static RowKind KindFor(Option option, OptionEntry entry = null)
        {
            var presentation = entry?.Presentation ?? option.Presentation;
            switch (presentation)
            {
                case OptionPresentation.Toggle: return RowKind.Toggle;
                case OptionPresentation.Slider: return RowKind.Slider;
                case OptionPresentation.Stepper: return RowKind.Stepper;
                case OptionPresentation.Dropdown: return RowKind.Dropdown;
                case OptionPresentation.Text: return RowKind.Text;
            }

            if (option is BoolOption)
                return RowKind.Toggle;
            if (option is IChoiceOption)
                return RowKind.Stepper;
            if (option is IRangeOption)
                return RowKind.Slider;
            return RowKind.Text;
        }

        /// <summary>Shows or hides rows by their current visibility rules, refreshes them, and rewires navigation.</summary>
        public static void RefreshRows(IList<OptionRow> rows, Selectable above = null, Selectable below = null)
        {
            foreach (var row in rows)
            {
                if (row == null)
                    continue;
                bool show = row.Context.ShouldShow(row.Option);
                if (row.gameObject.activeSelf != show)
                    row.gameObject.SetActive(show);
                row.Refresh();
            }

            WireNavigation(rows, above, below);
        }

        /// <summary>Explicit up/down between the active, interactable rows' primary selectables; left/right stay with the rows.</summary>
        public static void WireNavigation(IList<OptionRow> rows, Selectable above = null, Selectable below = null)
        {
            var active = new List<Selectable>();
            foreach (var row in rows)
            {
                if (row != null && row.gameObject.activeSelf && row.Primary != null && row.Primary.interactable)
                    active.Add(row.Primary);
            }

            for (int i = 0; i < active.Count; i++)
            {
                var navigation = active[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = i > 0 ? active[i - 1] : above;
                navigation.selectOnDown = i < active.Count - 1 ? active[i + 1] : below;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
                active[i].navigation = navigation;
            }

            if (above != null)
            {
                var navigation = above.navigation;
                navigation.selectOnDown = active.Count > 0 ? active[0] : below;
                above.navigation = navigation;
            }
        }

        /// <summary>The first active row with a selectable, or null.</summary>
        public static OptionRow FirstSelectable(IList<OptionRow> rows)
        {
            foreach (var row in rows)
            {
                if (row != null && row.gameObject.activeSelf && row.Primary != null && row.Primary.interactable)
                    return row;
            }

            return null;
        }

        GameObject SpawnHeader(Transform content, string text)
        {
            var header = Theme != null && Theme.headerRow != null
                ? UnityEngine.Object.Instantiate(Theme.headerRow, content)
                : DefaultOptionsUI.CreateHeader(content);
            var label = header.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = text;
            header.name = "Header: " + text;
            return header;
        }

        GameObject SpawnSpacer(Transform content, float height)
        {
            var spacer = Theme != null && Theme.spacerRow != null
                ? UnityEngine.Object.Instantiate(Theme.spacerRow, content)
                : DefaultOptionsUI.CreateSpacer(content, height);
            var element = spacer.GetComponent<LayoutElement>();
            if (element != null)
                element.preferredHeight = height;
            return spacer;
        }
    }
}
