using System;
using System.Collections.Generic;
using System.IO;
using toolbox.Options;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.UI;

namespace toolbox.Samples.OptionsDemo
{
    /// <summary>
    /// A consumer of the options module, end to end: a catalogue plus the standard packs in one store, appliers
    /// through <c>Bind</c>, a code layout with sections, headers, a spacer and a custom button, a runtime theme that
    /// overrides one row by id and one by custom presentation, a custom widget (<see cref="BlockMeterRow"/>), a text
    /// provider, feedback, and the built-in menu. The log at the bottom-left shows what the appliers receive.
    /// Press Play; keyboard, mouse and gamepad all work.
    /// </summary>
    public class OptionsDemo : MonoBehaviour
    {
        [Tooltip("The canvas (or any RectTransform) that hosts the menu.")]
        [SerializeField] RectTransform menuParent;
        [Tooltip("Use the code layout below; off uses the layout asset, or one page per category when that is empty too.")]
        [SerializeField] bool codeLayout = true;
        [SerializeField] OptionsLayoutAsset layoutAsset;
        [Tooltip("Optional theme asset; empty builds a small runtime theme with two overrides.")]
        [SerializeField] OptionsTheme theme;
        [SerializeField] bool shoutPageLabels = true;

        readonly List<IDisposable> bindings = new List<IDisposable>();
        readonly List<string> log = new List<string>();
        OptionsStore store;
        OptionsMenu menu;
        DisplayOptionsPack display;
        QualityOptionsPack quality;
        Transform templates;

        void Start()
        {
            // 1. One store: this catalogue, the standard packs, a demo-specific file.
            store = new OptionsStore(new FileOptionsStorage(Path.Combine(Application.persistentDataPath, "options-demo.json")));
            store.RegisterCatalog(typeof(DemoOptions));
            display = store.AddDisplay();
            quality = store.AddQuality();

            // 2. Appliers. Bound before Load, so each runs once with the loaded value and then on every change.
            bindings.Add(DemoOptions.MasterVolume.Bind(v => { AudioListener.volume = v; Log($"AudioListener.volume = {v:0.00}"); }));
            bindings.Add(DemoOptions.MusicVolume.Bind(v => Log($"music bus = {v:0.00}")));
            bindings.Add(DemoOptions.SfxVolume.Bind(v => Log($"sfx bus = {v:0.00}")));
            bindings.Add(DemoOptions.MuteInBackground.Bind(v => Log($"mute in background = {v}")));
            bindings.Add(DemoOptions.ScreenShake.Bind(v => Log($"screen shake = {v:0.0}")));
            bindings.Add(DemoOptions.DialogueSpeed.Bind(v => Log($"text speed = {v}")));
            bindings.Add(DemoOptions.PilotName.Bind(v => Log($"pilot = \"{v}\"")));
            bindings.Add(display.Mode.Bind(v => Log($"display mode = {v} ({DisplayOptionsPack.ResolveMode(v)})")));
            bindings.Add(display.Resolution.Bind(v => Log($"resolution = {v}")));
            bindings.Add(quality.Quality.Bind(v => Log($"quality = {v}")));

            store.Load();
            Log($"loaded {store.Count} options from {((FileOptionsStorage)store.Storage).Path}");

            // 3. The menu: built-in look, this store, and the hooks.
            menu = OptionsMenu.CreateDefault(menuParent != null ? menuParent : transform, "Options Demo");
            menu.Store = store;
            menu.Layout = codeLayout ? BuildLayout() : layoutAsset != null ? layoutAsset.ToLayout() : null;
            menu.Theme = theme != null ? theme : BuildRuntimeTheme();
            menu.TextProvider = shoutPageLabels ? new ShoutingTextProvider() : null;
            menu.Feedback = new LogFeedback(this);
            menu.BuilderCreated += builder => builder.RowCreated += (row, entry) =>
            {
                if (row.Option.HasFlag(OptionFlags.RequiresRestart))
                    Log($"row '{row.Option.Id}' needs a restart to apply; a game would add a marker here");
            };
            menu.Closed += () =>
            {
                Log("menu closed (Back or Cancel); reopen from the log box");
                menu.gameObject.SetActive(false);
            };
            menu.Open();
        }

        void OpenMenu()
        {
            menu.gameObject.SetActive(true);
            menu.Open();
        }

        void OnDestroy()
        {
            foreach (var binding in bindings)
                binding.Dispose();
            display?.Dispose();
            quality?.Dispose();
            store?.Dispose();
        }

        /// <summary>Pages, sections, headers, a spacer and a custom element, all in code.</summary>
        OptionsLayout BuildLayout()
        {
            var layout = new OptionsLayout();

            layout.Page("display", "Display")
                .Section("Window")
                .Options(DisplayOptionsPack.ModeId, DisplayOptionsPack.ResolutionId, DisplayOptionsPack.MonitorId)
                .Section("Performance")
                .Options(DisplayOptionsPack.VSyncId, DisplayOptionsPack.FpsCapId, QualityOptionsPack.QualityId);

            layout.Page("audio", "Audio")
                .Options(DemoOptions.MasterVolume.Id, DemoOptions.MusicVolume.Id, DemoOptions.SfxVolume.Id)
                .Spacer()
                .Option(DemoOptions.MuteInBackground.Id, entry => entry.Description = "Overridden per placement: this text comes from the layout, not the option.");

            layout.Page("gameplay", "Gameplay")
                .Section("Feel")
                .Options(DemoOptions.ScreenShake.Id, DemoOptions.DamageNumbers.Id)
                .Section("Text")
                .Options(DemoOptions.DialogueSpeed.Id, DemoOptions.Subtitles.Id, DemoOptions.SubtitleSize.Id)
                .Section("Saving")
                .Option(DemoOptions.AutosaveMinutes.Id);

            layout.Page("profile", "Profile")
                .Options(DemoOptions.PilotName.Id, DemoOptions.Telemetry.Id)
                .Spacer(20f)
                .Custom(CreditsButtonTemplate(), instance =>
                {
                    instance.SetActive(true);
                    instance.GetComponent<Button>().onClick.AddListener(() => Log("credits clicked"));
                })
                .Section("Debug")
                .Option(DemoOptions.ShowFps.Id);

            return layout;
        }

        /// <summary>A theme made at runtime: one row overridden by id (an accented master slider) and one custom presentation ("meter").</summary>
        OptionsTheme BuildRuntimeTheme()
        {
            var runtimeTheme = ScriptableObject.CreateInstance<OptionsTheme>();
            runtimeTheme.name = "Demo Theme (runtime)";

            var accentSlider = DefaultOptionsUI.CreateRow(RowKind.Slider, Templates());
            accentSlider.name = "Accent Slider Template";
            accentSlider.GetComponent<Image>().color = new Color(0.35f, 0.75f, 1f, 0.12f);
            runtimeTheme.rowOverrides.Add(new OptionsTheme.PrefabOverride { key = DemoOptions.MasterVolume.Id, prefab = accentSlider });

            var meter = BlockMeterRow.Create(Templates());
            meter.name = "Meter Template";
            runtimeTheme.customPresentations.Add(new OptionsTheme.PrefabOverride { key = "meter", prefab = meter });
            return runtimeTheme;
        }

        GameObject CreditsButtonTemplate()
        {
            var button = DefaultOptionsUI.Button(Templates(), "Credits", "Credits...", -1f);
            button.gameObject.SetActive(false);
            return button.gameObject;
        }

        /// <summary>An inactive holder for runtime-built "prefabs"; Instantiate copies them like assets.</summary>
        Transform Templates()
        {
            if (templates == null)
            {
                var holder = new GameObject("Templates (inactive)", typeof(RectTransform));
                holder.transform.SetParent(transform, false);
                holder.SetActive(false);
                templates = holder.transform;
            }

            return templates;
        }

        void Log(string line)
        {
            log.Add(line);
            if (log.Count > 10)
                log.RemoveAt(0);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10f, 10f, 420f, 260f), GUI.skin.box);
            GUILayout.Label("<b>Appliers</b>  (what Bind delivered)", new GUIStyle(GUI.skin.label) { richText = true });
            foreach (var line in log)
                GUILayout.Label(line);
            if (menu != null && !menu.IsOpen && GUILayout.Button("Open options"))
                OpenMenu();
            GUILayout.EndArea();
        }

        /// <summary>A text provider that shouts page labels: the hook a localisation layer would use.</summary>
        sealed class ShoutingTextProvider : DefaultOptionTextProvider
        {
            public override string PageLabel(OptionsPage page) => base.PageLabel(page).ToUpperInvariant();
        }

        /// <summary>Feedback into the log; a game plays sounds here.</summary>
        sealed class LogFeedback : IOptionsMenuFeedback
        {
            readonly OptionsDemo demo;

            public LogFeedback(OptionsDemo demo)
            {
                this.demo = demo;
            }

            public void OnRowSelected(OptionRow row)
            {
            }

            public void OnValueChanged(OptionRow row)
            {
            }

            public void OnValueCommitted(OptionRow row) => demo.Log($"committed {row.Option.Id} = {row.Option.Serialize()}");

            public void OnDenied(OptionRow row) => demo.Log($"denied on {row.Option.Id}");

            public void OnPageShown(int pageIndex) => demo.Log($"page {pageIndex}");
        }
    }
}
