using System;

namespace toolbox.Options.UI
{
    /// <summary>Every string a menu shows. Route it through a localisation system by replacing the default.</summary>
    public interface IOptionTextProvider
    {
        string Label(Option option, OptionEntry entry);
        string Description(Option option, OptionEntry entry);
        string ChoiceLabel(Option option, Choice choice);
        /// <summary>The committed value as shown next to a slider or in a summary.</summary>
        string Value(Option option);
        string PageLabel(OptionsPage page);
        string SectionLabel(OptionsSection section);
    }

    /// <summary>Entry overrides first, then the option's own metadata.</summary>
    public class DefaultOptionTextProvider : IOptionTextProvider
    {
        public virtual string Label(Option option, OptionEntry entry) => entry?.Label ?? option.Label;
        public virtual string Description(Option option, OptionEntry entry) => entry?.Description ?? option.Description ?? "";
        public virtual string ChoiceLabel(Option option, Choice choice) => choice.Label;
        public virtual string Value(Option option) => option.DisplayValue;
        public virtual string PageLabel(OptionsPage page) => page.Label ?? page.Id;
        public virtual string SectionLabel(OptionsSection section) => section.Label ?? "";
    }

    /// <summary>Sounds and other feedback. The menu never plays audio itself.</summary>
    public interface IOptionsMenuFeedback
    {
        void OnRowSelected(OptionRow row);
        /// <summary>A value changed, possibly live (a slider mid-drag).</summary>
        void OnValueChanged(OptionRow row);
        /// <summary>A value was committed (slider released, toggle flipped, choice stepped).</summary>
        void OnValueCommitted(OptionRow row);
        /// <summary>Input was rejected (text that does not parse, stepping past an end).</summary>
        void OnDenied(OptionRow row);
        void OnPageShown(int pageIndex);
    }

    /// <summary>"Keep these settings?" with a countdown. The default is <see cref="DefaultConfirmPrompt"/>.</summary>
    public interface IConfirmPrompt
    {
        bool IsShowing { get; }
        void Show(string message, float seconds, Action keep, Action revert);
        void Hide();
    }
}
