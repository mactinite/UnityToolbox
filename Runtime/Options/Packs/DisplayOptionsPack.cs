using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>
    /// The engine-level display options every desktop game needs, with their applier: display mode, resolution,
    /// monitor, vsync and frame cap. Add it with <see cref="StandardOptions.AddDisplay"/>. Mode, resolution and monitor
    /// carry <see cref="OptionFlags.Confirm"/>, so a menu stages them in an <see cref="OptionsTransaction"/> and
    /// offers to revert after <see cref="Config.RevertSeconds"/>.
    /// </summary>
    public sealed class DisplayOptionsPack : IDisposable
    {
        public sealed class Config
        {
            public string Category = "Display";
            public int OrderBase = 0;
            /// <summary>The mode on first run: <see cref="Borderless"/>, <see cref="Fullscreen"/> or <see cref="Windowed"/>.</summary>
            public string DefaultMode = Borderless;
            public bool IncludeMonitor = true;
            public bool IncludeVSync = true;
            public bool IncludeFpsCap = true;
            public int[] FpsCaps = { 30, 60, 120, 144, 240 };
            /// <summary>Seconds a menu should wait before reverting an unconfirmed display change.</summary>
            public float RevertSeconds = 15f;
            /// <summary>Also apply window changes in the editor, where they only resize the Game view.</summary>
            public bool ApplyInEditor = false;
        }

        public const string ModeId = "display.mode";
        public const string ResolutionId = "display.resolution";
        public const string MonitorId = "display.monitor";
        public const string VSyncId = "display.vsync";
        public const string FpsCapId = "display.fpsCap";

        public const string Borderless = "Borderless";
        public const string Fullscreen = "Fullscreen";
        public const string Windowed = "Windowed";
        public const string FpsUnlimited = "Off";

        readonly List<DisplayInfo> displays = new List<DisplayInfo>();
        readonly List<IDisposable> bindings = new List<IDisposable>();

        internal DisplayOptionsPack(OptionsStore store, Config config)
        {
            Settings = config ?? new Config();
            RefreshDisplayList();
            int currentDisplay = CurrentDisplayIndex();
            int order = Settings.OrderBase;

            Mode = new ChoiceOption(ModeId, Settings.DefaultMode, ModeChoices())
            {
                Category = Settings.Category,
                Order = order++,
                Label = "Display mode",
                Description = "Borderless fills the screen at the desktop resolution. Windowed uses the resolution below.",
                Flags = OptionFlags.Confirm,
                Presentation = OptionPresentation.Stepper,
            };

            Resolution = new ChoiceOption(ResolutionId, NativeResolutionKey(currentDisplay), ResolutionChoices(currentDisplay))
            {
                Category = Settings.Category,
                Order = order++,
                Label = "Resolution",
                Flags = OptionFlags.Confirm,
                Presentation = OptionPresentation.Stepper,
                VisibleWhen = () => Mode.Value != Borderless,
            };

            if (Settings.IncludeMonitor)
            {
                Monitor = new ChoiceOption(MonitorId, currentDisplay.ToString(CultureInfo.InvariantCulture), MonitorChoices())
                {
                    Category = Settings.Category,
                    Order = order++,
                    Label = "Monitor",
                    Flags = OptionFlags.Confirm,
                    Presentation = OptionPresentation.Stepper,
                    VisibleWhen = () => displays.Count > 1,
                };
            }

            if (Settings.IncludeVSync)
            {
                VSync = new BoolOption(VSyncId, true)
                {
                    Category = Settings.Category,
                    Order = order++,
                    Label = "VSync",
                    Description = "Synchronise frames with the monitor to avoid tearing.",
                };
            }

            if (Settings.IncludeFpsCap)
            {
                FpsCap = new ChoiceOption(FpsCapId, FpsUnlimited, FpsChoices())
                {
                    Category = Settings.Category,
                    Order = order++,
                    Label = "Frame rate limit",
                    Description = "Only applies while VSync is off.",
                    Presentation = OptionPresentation.Stepper,
                    EnabledWhen = () => VSync == null || !VSync.Value,
                };
            }

            store.Register(Mode, Resolution, Monitor, VSync, FpsCap);

            bindings.Add(Mode.Bind(_ => ApplyWindow()));
            bindings.Add(Resolution.Bind(_ => ApplyWindow()));
            if (Monitor != null)
                bindings.Add(Monitor.Bind(_ => OnMonitorChanged()));
            if (VSync != null)
                bindings.Add(VSync.Bind(_ => ApplyFrameRate()));
            if (FpsCap != null)
                bindings.Add(FpsCap.Bind(_ => ApplyFrameRate()));

            // A quality level change rewrites vSyncCount; put the option's value back.
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;
        }

        public Config Settings { get; }
        public ChoiceOption Mode { get; }
        public ChoiceOption Resolution { get; }
        /// <summary>Null when <see cref="Config.IncludeMonitor"/> is false.</summary>
        public ChoiceOption Monitor { get; }
        /// <summary>Null when <see cref="Config.IncludeVSync"/> is false.</summary>
        public BoolOption VSync { get; }
        /// <summary>Null when <see cref="Config.IncludeFpsCap"/> is false.</summary>
        public ChoiceOption FpsCap { get; }

        /// <summary>Connected displays as of the last <see cref="RefreshDisplays"/>.</summary>
        public IReadOnlyList<DisplayInfo> Displays => displays;

        /// <summary>Exclusive fullscreen exists on Windows; elsewhere "Fullscreen" is offered as borderless only.</summary>
        public static bool SupportsExclusiveFullscreen =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        public static string ResolutionKey(int width, int height) =>
            width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);

        public static bool TryParseResolution(string key, out int width, out int height)
        {
            width = height = 0;
            if (string.IsNullOrEmpty(key))
                return false;
            int separator = key.IndexOf('x');
            return separator > 0
                && int.TryParse(key.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out width)
                && int.TryParse(key.Substring(separator + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out height)
                && width > 0 && height > 0;
        }

        /// <summary>Re-reads the connected displays (after a monitor was plugged in) and refreshes the monitor and resolution choices.</summary>
        public void RefreshDisplays()
        {
            RefreshDisplayList();
            Monitor?.SetChoices(MonitorChoices());
            RefreshResolutionChoices();
        }

        /// <summary>Applies mode, resolution and monitor from the current values. Bindings call this; menus rarely need to.</summary>
        public void ApplyWindow()
        {
            if (Application.isEditor && !Settings.ApplyInEditor)
                return;

            int displayIndex = SelectedDisplayIndex();
            var display = displays[displayIndex];
            if (displayIndex != CurrentDisplayIndex())
                Screen.MoveMainWindowTo(in display, Vector2Int.zero);

            var mode = ResolveMode(Mode.Value);
            int width, height;
            if (Mode.Value == Borderless || !TryParseResolution(Resolution.Value, out width, out height))
            {
                width = display.width > 0 ? display.width : Screen.currentResolution.width;
                height = display.height > 0 ? display.height : Screen.currentResolution.height;
            }

            Screen.SetResolution(width, height, mode, BestRefreshRate(display, width, height));
        }

        /// <summary>Applies vsync and the frame cap from the current values.</summary>
        public void ApplyFrameRate()
        {
            bool vsync = VSync != null ? VSync.Value : QualitySettings.vSyncCount > 0;
            if (VSync != null)
                QualitySettings.vSyncCount = vsync ? 1 : 0;
            if (FpsCap == null)
                return;

            int cap = -1;
            if (vsync || !int.TryParse(FpsCap.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out cap))
                cap = -1;
            Application.targetFrameRate = cap;
        }

        public static FullScreenMode ResolveMode(string modeKey)
        {
            switch (modeKey)
            {
                case Fullscreen:
                    return SupportsExclusiveFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
                case Windowed:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        void OnMonitorChanged()
        {
            RefreshResolutionChoices();
            ApplyWindow();
        }

        void OnQualityLevelChanged(int previousLevel, int currentLevel) => ApplyFrameRate();

        void RefreshDisplayList()
        {
            displays.Clear();
            Screen.GetDisplayLayout(displays);
            if (displays.Count == 0)
            {
                var current = Screen.currentResolution;
                displays.Add(new DisplayInfo
                {
                    name = "Display",
                    width = current.width,
                    height = current.height,
                    refreshRate = current.refreshRateRatio,
                });
            }
        }

        void RefreshResolutionChoices()
        {
            if (Resolution == null)
                return;
            int displayIndex = SelectedDisplayIndex();
            string previous = Resolution.Value;
            Resolution.SetChoices(ResolutionChoices(displayIndex));
            if (Resolution.IndexOf(previous) < 0)
                Resolution.Value = NativeResolutionKey(displayIndex);
        }

        int CurrentDisplayIndex()
        {
            var main = Screen.mainWindowDisplayInfo;
            for (int i = 0; i < displays.Count; i++)
            {
                var display = displays[i];
                if (display.name == main.name && display.width == main.width && display.height == main.height)
                    return i;
            }

            return 0;
        }

        int SelectedDisplayIndex()
        {
            if (Monitor != null && int.TryParse(Monitor.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                return Mathf.Clamp(index, 0, displays.Count - 1);
            return CurrentDisplayIndex();
        }

        string NativeResolutionKey(int displayIndex)
        {
            var display = displays[Mathf.Clamp(displayIndex, 0, displays.Count - 1)];
            return ResolutionKey(display.width, display.height);
        }

        IEnumerable<Choice> ModeChoices()
        {
            yield return new Choice(Borderless);
            if (SupportsExclusiveFullscreen)
                yield return new Choice(Fullscreen);
            yield return new Choice(Windowed);
        }

        IEnumerable<Choice> ResolutionChoices(int displayIndex)
        {
            var display = displays[Mathf.Clamp(displayIndex, 0, displays.Count - 1)];
            var modes = ModesFor(display);

            var sizes = new HashSet<(int width, int height)>();
            foreach (var mode in modes)
                sizes.Add((mode.width, mode.height));
            if (display.width > 0 && display.height > 0)
                sizes.Add((display.width, display.height));

            foreach (var size in sizes.OrderBy(size => size.width).ThenBy(size => size.height))
                yield return new Choice(ResolutionKey(size.width, size.height), size.width + " × " + size.height);
        }

        IEnumerable<Choice> MonitorChoices()
        {
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < displays.Count; i++)
            {
                string name = string.IsNullOrEmpty(displays[i].name) ? "Display" : displays[i].name;
                seen.TryGetValue(name, out int count);
                seen[name] = count + 1;
                string label = count == 0 ? name : name + " (" + (count + 1) + ")";
                yield return new Choice(i.ToString(CultureInfo.InvariantCulture), label);
            }
        }

        IEnumerable<Choice> FpsChoices()
        {
            yield return new Choice(FpsUnlimited);
            foreach (int cap in Settings.FpsCaps ?? Array.Empty<int>())
            {
                string key = cap.ToString(CultureInfo.InvariantCulture);
                yield return new Choice(key, key + " fps");
            }
        }

        /// <summary>The full-screen modes of a display. Per-display lists are not supported everywhere; then the main display's list is used.</summary>
        static Resolution[] ModesFor(DisplayInfo display)
        {
            Resolution[] modes = null;
            try
            {
                modes = display.resolutions;
            }
            catch (NotSupportedException)
            {
            }

            return modes != null && modes.Length > 0 ? modes : Screen.resolutions ?? Array.Empty<Resolution>();
        }

        static RefreshRate BestRefreshRate(DisplayInfo display, int width, int height)
        {
            var modes = ModesFor(display);

            RefreshRate best = Screen.currentResolution.refreshRateRatio;
            bool found = false;
            foreach (var mode in modes)
            {
                if (mode.width != width || mode.height != height)
                    continue;
                if (!found || mode.refreshRateRatio.value > best.value)
                {
                    best = mode.refreshRateRatio;
                    found = true;
                }
            }

            return best;
        }

        public void Dispose()
        {
            QualitySettings.activeQualityLevelChanged -= OnQualityLevelChanged;
            foreach (var binding in bindings)
                binding.Dispose();
            bindings.Clear();
        }
    }
}
