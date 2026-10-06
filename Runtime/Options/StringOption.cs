namespace toolbox.Options
{
    /// <summary>Free text, or an opaque blob another system owns (input binding overrides, a language code).</summary>
    public class StringOption : Option<string>
    {
        public StringOption(string id, string defaultValue = "") : base(id, defaultValue ?? "")
        {
        }

        /// <summary>Longest accepted text, or 0 for unlimited.</summary>
        public int MaxLength { get; set; }

        protected override string Coerce(string candidate)
        {
            candidate = candidate ?? "";
            if (MaxLength > 0 && candidate.Length > MaxLength)
                candidate = candidate.Substring(0, MaxLength);
            return candidate;
        }

        protected override string ToText(string value) => value ?? "";

        protected override bool TryParseText(string text, out string value)
        {
            value = text ?? "";
            return true;
        }
    }
}
