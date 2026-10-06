namespace toolbox.Options
{
    /// <summary>An on/off option. Persists as "true"/"false"; also reads 1/0, on/off and yes/no.</summary>
    public class BoolOption : Option<bool>
    {
        public BoolOption(string id, bool defaultValue = false) : base(id, defaultValue)
        {
        }

        public string OnLabel { get; set; } = "On";
        public string OffLabel { get; set; } = "Off";

        protected override string ToText(bool value) => value ? "true" : "false";

        protected override bool TryParseText(string text, out bool value)
        {
            switch (text?.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "on":
                case "yes":
                    value = true;
                    return true;
                case "false":
                case "0":
                case "off":
                case "no":
                    value = false;
                    return true;
            }

            value = false;
            return false;
        }

        protected override string FormatValue(bool value) => value ? OnLabel : OffLabel;
    }
}
