using toolbox.Options;

namespace toolbox.Samples.OptionsDemo
{
    public enum TextSpeed
    {
        Slow,
        Normal,
        Fast,
        Instant,
    }

    /// <summary>
    /// A game's catalogue: every option declared once, with the metadata a menu needs. The display and quality
    /// options are not here; the standard packs add them in <see cref="OptionsDemo"/>.
    /// </summary>
    [OptionsCatalog]
    public static class DemoOptions
    {
        // Audio: 0..1 floats shown as percentages. The master slider gets a custom row through the theme.
        public static readonly FloatOption MasterVolume = new FloatOption("audio.master", 1f)
        {
            Category = "Audio", Label = "Master volume", Format = OptionFormat.Percent,
            Description = "Everything, including this menu.",
        };

        public static readonly FloatOption MusicVolume = new FloatOption("audio.music", 0.8f)
        {
            Category = "Audio", Label = "Music", Format = OptionFormat.Percent, Order = 1,
        };

        public static readonly FloatOption SfxVolume = new FloatOption("audio.sfx", 1f)
        {
            Category = "Audio", Label = "Sound effects", Format = OptionFormat.Percent, Order = 2,
        };

        public static readonly BoolOption MuteInBackground = new BoolOption("audio.muteInBackground", true)
        {
            Category = "Audio", Label = "Mute in background", Order = 3,
            Description = "Silence the game while another window has focus.",
        };

        // Gameplay: a custom presentation, an enum, a dependent option and a stepped integer.
        public static readonly FloatOption ScreenShake = new FloatOption("gameplay.screenShake", 1f, 0f, 1f, 0.1f)
        {
            Category = "Gameplay", Label = "Screen shake",
            Presentation = OptionPresentation.Custom, CustomPresentation = "meter",
            Description = "Camera shake on impacts. Drawn by the sample's BlockMeterRow.",
        };

        public static readonly BoolOption DamageNumbers = new BoolOption("gameplay.damageNumbers", true)
        {
            Category = "Gameplay", Label = "Damage numbers", Order = 1,
        };

        public static readonly EnumOption<TextSpeed> DialogueSpeed = new EnumOption<TextSpeed>("gameplay.textSpeed", TextSpeed.Normal)
        {
            Category = "Gameplay", Label = "Text speed", Order = 2,
            Description = "How fast dialogue types out. Stored by name, so the enum can be reordered.",
        };

        public static readonly BoolOption Subtitles = new BoolOption("gameplay.subtitles", true)
        {
            Category = "Gameplay", Label = "Subtitles", Order = 3,
        };

        public static readonly ChoiceOption SubtitleSize = new ChoiceOption("gameplay.subtitleSize", "Medium", "Small", "Medium", "Large")
        {
            Category = "Gameplay", Label = "Subtitle size", Order = 4,
            VisibleWhen = () => Subtitles.Value,
            Description = "Only shown while subtitles are on.",
        };

        public static readonly IntOption AutosaveMinutes = new IntOption("gameplay.autosaveMinutes", 5, 1, 30)
        {
            Category = "Gameplay", Label = "Autosave interval", Suffix = " min", Order = 5,
            Presentation = OptionPresentation.Stepper,
        };

        // Profile: free text and a restart-flagged toggle.
        public static readonly StringOption PilotName = new StringOption("profile.name", "Pilot")
        {
            Category = "Profile", Label = "Pilot name", MaxLength = 16,
            Description = "Shown on the leaderboard. Up to 16 characters.",
        };

        public static readonly BoolOption Telemetry = new BoolOption("profile.telemetry")
        {
            Category = "Profile", Label = "Share anonymous stats", Order = 1,
            Flags = OptionFlags.RequiresRestart,
            Description = "Takes effect after a restart.",
        };

        // Debug: only in the editor and development builds.
        public static readonly BoolOption ShowFps = new BoolOption("debug.showFps")
        {
            Category = "Debug", Label = "FPS counter", Flags = OptionFlags.DevOnly,
        };
    }
}
