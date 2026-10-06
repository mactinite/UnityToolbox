namespace toolbox.Options
{
    /// <summary>Ready-made option packs with their appliers, so a game gets the engine-level settings with one call each.</summary>
    public static class StandardOptions
    {
        /// <summary>Display mode, resolution, monitor, vsync and frame cap. See <see cref="DisplayOptionsPack"/>.</summary>
        public static DisplayOptionsPack AddDisplay(this OptionsStore store, DisplayOptionsPack.Config config = null) =>
            new DisplayOptionsPack(store, config);

        /// <summary>The project's quality levels. See <see cref="QualityOptionsPack"/>.</summary>
        public static QualityOptionsPack AddQuality(this OptionsStore store, string category = "Graphics") =>
            new QualityOptionsPack(store, category);

        /// <summary>The ids the packs register, for editor tooling that lists ids before the packs exist at runtime.</summary>
        public static readonly string[] PackIds =
        {
            DisplayOptionsPack.ModeId,
            DisplayOptionsPack.ResolutionId,
            DisplayOptionsPack.MonitorId,
            DisplayOptionsPack.VSyncId,
            DisplayOptionsPack.FpsCapId,
            QualityOptionsPack.QualityId,
        };
    }
}
