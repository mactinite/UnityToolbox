using System;
using System.Globalization;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>A whole-number option with an optional range and step.</summary>
    public class IntOption : Option<int>, IRangeOption
    {
        public IntOption(string id, int defaultValue, int min = int.MinValue, int max = int.MaxValue, int step = 1)
            : base(id, defaultValue)
        {
            if (min > max)
                throw new ArgumentException($"Option '{id}': min {min} is greater than max {max}.", nameof(min));
            if (defaultValue < min || defaultValue > max)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), $"Option '{id}': default {defaultValue} is outside {min}..{max}.");
            Min = min;
            Max = max;
            Step = Math.Max(1, step);
        }

        public int Min { get; }
        public int Max { get; }
        public int Step { get; }

        /// <summary>Appended to the displayed value (" fps", "%").</summary>
        public string Suffix { get; set; }

        public bool HasBounds => Min != int.MinValue && Max != int.MaxValue;

        protected override int Coerce(int candidate)
        {
            candidate = Mathf.Clamp(candidate, Min, Max);
            if (Step > 1 && Min != int.MinValue)
            {
                long steps = (long)Math.Round((candidate - (double)Min) / Step);
                candidate = (int)Math.Min(Max, Min + steps * Step);
            }

            return candidate;
        }

        protected override string ToText(int value) => value.ToString(CultureInfo.InvariantCulture);

        protected override bool TryParseText(string text, out int value) =>
            int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        protected override string FormatValue(int value) => ToText(value) + Suffix;

        float IRangeOption.MinValue => Min;
        float IRangeOption.MaxValue => Max;
        float IRangeOption.StepValue => Step;
        bool IRangeOption.IsInteger => true;

        float IRangeOption.FloatValue
        {
            get => Value;
            set => Value = Mathf.RoundToInt(value);
        }
    }
}
