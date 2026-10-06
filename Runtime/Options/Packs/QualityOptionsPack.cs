using System;
using System.Linq;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>
    /// <c>graphics.quality</c>: the project's quality levels by name, applied with
    /// <see cref="QualitySettings.SetQualityLevel(int, bool)"/>. Add it with <see cref="StandardOptions.AddQuality"/>.
    /// </summary>
    public sealed class QualityOptionsPack : IDisposable
    {
        public const string QualityId = "graphics.quality";

        readonly string[] names;
        readonly IDisposable binding;

        internal QualityOptionsPack(OptionsStore store, string category)
        {
            names = QualitySettings.names ?? Array.Empty<string>();
            int current = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Math.Max(0, names.Length - 1));
            string defaultName = names.Length > 0 ? names[current] : "";

            Quality = new ChoiceOption(QualityId, defaultName, names.Select(name => new Choice(name)))
            {
                Category = category,
                Label = "Quality",
                Description = "Rendering quality preset.",
                Presentation = OptionPresentation.Stepper,
            };

            store.Register(Quality);
            binding = Quality.Bind(Apply);
        }

        public ChoiceOption Quality { get; }

        void Apply(string levelName)
        {
            int index = Array.IndexOf(names, levelName);
            if (index >= 0 && index != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(index, true);
        }

        public void Dispose() => binding?.Dispose();
    }
}
