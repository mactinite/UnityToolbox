using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.Options.UI
{
    /// <summary>The widget a row is built from. Resolved from the option type and <see cref="OptionPresentation"/> hints.</summary>
    public enum RowKind
    {
        Toggle,
        Slider,
        Stepper,
        Dropdown,
        Text,
        Keybind,
    }

    /// <summary>
    /// The look of a settings menu: one prefab per row kind, the chrome prefabs, and overrides for particular options.
    /// Every field may stay empty; the menu then builds that element from plain uGUI (<see cref="DefaultOptionsUI"/>).
    /// Row prefabs carry an <see cref="OptionRow"/> whose widgets are wired through serialized fields, so a prefab
    /// can be laid out any way.
    /// </summary>
    [CreateAssetMenu(menuName = "Toolbox/Options/Theme", fileName = "OptionsTheme")]
    public sealed class OptionsTheme : ScriptableObject
    {
        [Serializable]
        public struct PrefabOverride
        {
            public string key;
            public GameObject prefab;
        }

        [Header("Rows (empty = built-in plain uGUI row)")]
        public GameObject toggleRow;
        public GameObject sliderRow;
        public GameObject stepperRow;
        public GameObject dropdownRow;
        public GameObject textRow;
        public GameObject keybindRow;
        public GameObject headerRow;
        public GameObject spacerRow;

        [Header("Chrome (empty = built-in)")]
        public GameObject tabButton;
        [Tooltip("Must carry an IConfirmPrompt.")]
        public GameObject confirmPrompt;

        [Header("Overrides")]
        [Tooltip("Row prefabs for particular option ids.")]
        public List<PrefabOverride> rowOverrides = new List<PrefabOverride>();
        [Tooltip("Row prefabs for OptionPresentation.Custom keys (Option.CustomPresentation).")]
        public List<PrefabOverride> customPresentations = new List<PrefabOverride>();

        public GameObject RowOverrideFor(string optionId) => Find(rowOverrides, optionId);

        public GameObject CustomPresentationFor(string key) => Find(customPresentations, key);

        public GameObject RowPrefabFor(RowKind kind)
        {
            switch (kind)
            {
                case RowKind.Toggle: return toggleRow;
                case RowKind.Slider: return sliderRow;
                case RowKind.Stepper: return stepperRow;
                case RowKind.Dropdown: return dropdownRow;
                case RowKind.Text: return textRow;
                case RowKind.Keybind: return keybindRow;
                default: return null;
            }
        }

        static GameObject Find(List<PrefabOverride> overrides, string key)
        {
            if (string.IsNullOrEmpty(key) || overrides == null)
                return null;
            foreach (var entry in overrides)
            {
                if (entry.key == key && entry.prefab != null)
                    return entry.prefab;
            }

            return null;
        }
    }
}
