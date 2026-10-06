using System;
using System.Globalization;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>A number option with a range and an optional snap step. Defaults to 0..1, which suits volumes and strengths.</summary>
    public class FloatOption : Option<float>, IRangeOption
    {
        const float Tolerance = 1e-6f;

        public FloatOption(string id, float defaultValue, float min = 0f, float max = 1f, float step = 0f)
            : base(id, defaultValue)
        {
            if (min > max)
                throw new ArgumentException($"Option '{id}': min {min} is greater than max {max}.", nameof(min));
            if (defaultValue < min || defaultValue > max)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), $"Option '{id}': default {defaultValue} is outside {min}..{max}.");
            Min = min;
            Max = max;
            Step = Mathf.Max(0f, step);
        }

        public float Min { get; }
        public float Max { get; }
        public float Step { get; }

        public OptionFormat Format { get; set; }

        /// <summary>Appended to the displayed value (" s", " px"). Percent already carries its sign.</summary>
        public string Suffix { get; set; }

        protected override float Coerce(float candidate)
        {
            if (float.IsNaN(candidate))
                return Default;
            candidate = Mathf.Clamp(candidate, Min, Max);
            if (Step > 0f)
                candidate = Mathf.Clamp(Min + Mathf.Round((candidate - Min) / Step) * Step, Min, Max);
            return candidate;
        }

        protected override bool AreEqual(float a, float b) => Math.Abs(a - b) < Tolerance;

        // Seven decimals cover float precision without the round-trip noise of "R" (0.9f would otherwise save as 0.900000036).
        protected override string ToText(float value) => value.ToString("0.#######", CultureInfo.InvariantCulture);

        protected override bool TryParseText(string text, out float value) =>
            float.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && !float.IsNaN(value) && !float.IsInfinity(value);

        protected override string FormatValue(float value)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (Format)
            {
                case OptionFormat.Percent:
                    return Mathf.RoundToInt(Mathf.InverseLerp(Min, Max, value) * 100f).ToString(culture) + "%";
                case OptionFormat.Integer:
                    return Mathf.RoundToInt(value).ToString(culture) + Suffix;
                case OptionFormat.OneDecimal:
                    return value.ToString("0.0", culture) + Suffix;
                case OptionFormat.TwoDecimals:
                    return value.ToString("0.00", culture) + Suffix;
                default:
                    return value.ToString("0.##", culture) + Suffix;
            }
        }

        float IRangeOption.MinValue => Min;
        float IRangeOption.MaxValue => Max;
        float IRangeOption.StepValue => Step;
        bool IRangeOption.IsInteger => Format == OptionFormat.Integer;

        float IRangeOption.FloatValue
        {
            get => Value;
            set => Value = value;
        }
    }
}
