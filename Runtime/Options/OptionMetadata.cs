using System;
using System.Collections.Generic;

namespace toolbox.Options
{
    /// <summary>Behaviour flags that menus and the store honour.</summary>
    [Flags]
    public enum OptionFlags
    {
        None = 0,
        /// <summary>Never shown in a menu; still persisted and settable from code.</summary>
        Hidden = 1 << 0,
        /// <summary>Shown only in the editor and in development builds.</summary>
        DevOnly = 1 << 1,
        /// <summary>
        /// Changes are staged, applied together and then confirmed or reverted (display mode, resolution).
        /// See <see cref="OptionsTransaction"/>.
        /// </summary>
        Confirm = 1 << 2,
        /// <summary>The change only takes effect after a restart; menus should say so.</summary>
        RequiresRestart = 1 << 3,
    }

    /// <summary>
    /// A hint for the widget a menu should use. <see cref="Default"/> lets the theme pick by option type;
    /// <see cref="Custom"/> looks up <see cref="Option.CustomPresentation"/> in the theme.
    /// </summary>
    public enum OptionPresentation
    {
        Default,
        Toggle,
        Slider,
        Stepper,
        Dropdown,
        Text,
        /// <summary>A key/button binding; rows come from the input module (one per binding, all over one option).</summary>
        Keybind,
        Custom,
    }

    /// <summary>How a numeric value is written for display. Percent is the position within the option's range.</summary>
    public enum OptionFormat
    {
        Default,
        Percent,
        Integer,
        OneDecimal,
        TwoDecimals,
    }

    /// <summary>One entry of a choice-like option: a stable key that is persisted and a label that is shown.</summary>
    public readonly struct Choice : IEquatable<Choice>
    {
        public readonly string Key;
        readonly string label;

        public Choice(string key, string label = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            this.label = label;
        }

        public string Label => label ?? Key;

        public bool Equals(Choice other) => Key == other.Key;
        public override bool Equals(object obj) => obj is Choice other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();
        public override string ToString() => Label;

        public static implicit operator Choice(string key) => new Choice(key);
    }

    /// <summary>A numeric option a slider can drive without knowing its exact type.</summary>
    public interface IRangeOption
    {
        float MinValue { get; }
        float MaxValue { get; }
        /// <summary>Snap increment, or 0 for continuous.</summary>
        float StepValue { get; }
        /// <summary>Whether the value is shown as a whole number.</summary>
        bool IsInteger { get; }
        float FloatValue { get; set; }
    }

    /// <summary>An option with a finite list of choices that a stepper or dropdown can drive.</summary>
    public interface IChoiceOption
    {
        IReadOnlyList<Choice> Choices { get; }
        int SelectedIndex { get; set; }
        /// <summary>Whether <see cref="Next"/> and <see cref="Previous"/> wrap around the ends.</summary>
        bool Wrap { get; }
        void Next();
        void Previous();
    }

    /// <summary>
    /// Marks a static class whose static <see cref="Option"/> fields form a game's catalogue. Editor tooling lists
    /// their ids, and <see cref="OptionsStore.RegisterCatalog(Type)"/> registers them in declaration order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class OptionsCatalogAttribute : Attribute
    {
    }

    /// <summary>On a string field: the inspector offers the ids of every <see cref="OptionsCatalogAttribute"/> class, with free text as a fallback.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OptionIdAttribute : UnityEngine.PropertyAttribute
    {
    }
}
