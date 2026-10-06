using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>An enum-valued option. Persists the member name, so reordering the enum is safe; renaming a member needs a migration.</summary>
    public class EnumOption<TEnum> : Option<TEnum>, IChoiceOption where TEnum : struct, Enum
    {
        readonly List<TEnum> values;
        List<Choice> choices;
        Func<TEnum, string> labelFor;

        /// <param name="allowed">The members offered; all members when empty.</param>
        public EnumOption(string id, TEnum defaultValue, params TEnum[] allowed) : base(id, defaultValue)
        {
            values = allowed != null && allowed.Length > 0
                ? allowed.Distinct().ToList()
                : Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Distinct().ToList();
            if (!values.Contains(defaultValue))
                values.Insert(0, defaultValue);
            RebuildChoices();
        }

        /// <summary>Labels for the choices; the default humanizes the member names.</summary>
        public EnumOption<TEnum> WithLabels(Func<TEnum, string> labels)
        {
            labelFor = labels;
            RebuildChoices();
            return this;
        }

        public IReadOnlyList<Choice> Choices => choices;
        public IReadOnlyList<TEnum> Values => values;
        public bool Wrap { get; set; }

        public int SelectedIndex
        {
            get => values.IndexOf(Value);
            set
            {
                if (values.Count > 0)
                    Value = values[Mathf.Clamp(value, 0, values.Count - 1)];
            }
        }

        public void Next() => Step(1);
        public void Previous() => Step(-1);

        void Step(int delta)
        {
            int count = values.Count;
            int index = SelectedIndex + delta;
            index = Wrap ? ((index % count) + count) % count : Mathf.Clamp(index, 0, count - 1);
            SelectedIndex = index;
        }

        void RebuildChoices()
        {
            choices = values
                .Select(member => new Choice(member.ToString(), labelFor != null ? labelFor(member) : Humanize(member.ToString())))
                .ToList();
        }

        protected override TEnum Coerce(TEnum candidate) => values.Contains(candidate) ? candidate : Default;

        protected override string ToText(TEnum value) => value.ToString();

        protected override bool TryParseText(string text, out TEnum value) =>
            Enum.TryParse(text?.Trim(), true, out value) && values.Contains(value);

        protected override string FormatValue(TEnum value)
        {
            int index = values.IndexOf(value);
            return index >= 0 ? choices[index].Label : value.ToString();
        }
    }
}
