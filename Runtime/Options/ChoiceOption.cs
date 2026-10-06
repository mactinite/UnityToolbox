using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>
    /// One of a list of keyed choices. The key is persisted, the label is shown. Choices can be replaced at runtime
    /// (resolutions, monitors); a value whose key disappears falls back to the default, or the first choice.
    /// </summary>
    public class ChoiceOption : Option<string>, IChoiceOption
    {
        readonly List<Choice> choices = new List<Choice>();

        public ChoiceOption(string id, string defaultKey, params string[] choiceKeys)
            : this(id, defaultKey, choiceKeys?.Select(key => new Choice(key)))
        {
        }

        public ChoiceOption(string id, string defaultKey, IEnumerable<Choice> choices) : base(id, defaultKey ?? "")
        {
            if (choices != null)
                this.choices.AddRange(choices);
            Set(Default);
        }

        public IReadOnlyList<Choice> Choices => choices;

        /// <summary>Whether <see cref="Next"/> and <see cref="Previous"/> wrap around the ends.</summary>
        public bool Wrap { get; set; }

        /// <summary>Replaces the choices and re-validates the value (an unknown key falls back to the default or the first choice).</summary>
        public void SetChoices(IEnumerable<Choice> newChoices)
        {
            choices.Clear();
            if (newChoices != null)
                choices.AddRange(newChoices);
            Set(Value);
        }

        public int IndexOf(string key) => choices.FindIndex(choice => choice.Key == key);

        public int SelectedIndex
        {
            get => IndexOf(Value);
            set
            {
                if (choices.Count > 0)
                    Value = choices[Mathf.Clamp(value, 0, choices.Count - 1)].Key;
            }
        }

        public Choice Selected
        {
            get
            {
                int index = SelectedIndex;
                return index >= 0 ? choices[index] : new Choice(Value);
            }
        }

        public string SelectedLabel => Selected.Label;

        public void Next() => Step(1);
        public void Previous() => Step(-1);

        void Step(int delta)
        {
            int count = choices.Count;
            if (count == 0)
                return;
            int index = SelectedIndex + delta;
            index = Wrap ? ((index % count) + count) % count : Mathf.Clamp(index, 0, count - 1);
            SelectedIndex = index;
        }

        protected override string Coerce(string candidate)
        {
            if (choices.Count == 0)
                return candidate ?? "";
            if (IndexOf(candidate) >= 0)
                return candidate;
            return IndexOf(Default) >= 0 ? Default : choices[0].Key;
        }

        protected override string ToText(string value) => value ?? "";

        protected override bool TryParseText(string text, out string value)
        {
            value = text ?? "";
            return true;
        }

        protected override string FormatValue(string value) => SelectedLabel;
    }
}
