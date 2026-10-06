using System.Linq;
using NUnit.Framework;
using toolbox.Options;
using UnityEngine;

namespace toolbox.Tests
{
    /// <summary>
    /// The packs register their options and expose their helpers. The store is never loaded here, so the appliers
    /// never run and the editor's quality and window settings stay untouched.
    /// </summary>
    public class StandardOptionsTests
    {
        OptionsStore store;

        [SetUp]
        public void SetUp() => store = new OptionsStore(new InMemoryOptionsStorage());

        [TearDown]
        public void TearDown() => store.Dispose();

        [Test]
        public void AddDisplay_RegistersTheDisplayOptions_WithConfirmOnWindowChanges()
        {
            using (var pack = store.AddDisplay())
            {
                Assert.That(store.Get(DisplayOptionsPack.ModeId), Is.SameAs(pack.Mode));
                Assert.That(store.Get(DisplayOptionsPack.ResolutionId), Is.SameAs(pack.Resolution));
                Assert.That(store.Get(DisplayOptionsPack.MonitorId), Is.SameAs(pack.Monitor));
                Assert.That(store.Get(DisplayOptionsPack.VSyncId), Is.SameAs(pack.VSync));
                Assert.That(store.Get(DisplayOptionsPack.FpsCapId), Is.SameAs(pack.FpsCap));

                Assert.That(pack.Mode.HasFlag(OptionFlags.Confirm), Is.True);
                Assert.That(pack.Resolution.HasFlag(OptionFlags.Confirm), Is.True);
                Assert.That(pack.Monitor.HasFlag(OptionFlags.Confirm), Is.True);
                Assert.That(pack.VSync.HasFlag(OptionFlags.Confirm), Is.False);
                Assert.That(store.All.All(option => option.Category == "Display"), Is.True);
                Assert.That(pack.Displays.Count, Is.GreaterThan(0));
            }
        }

        [Test]
        public void AddDisplay_ChoicesComeFromTheMachine()
        {
            using (var pack = store.AddDisplay())
            {
                Assert.That(pack.Mode.Choices.Select(choice => choice.Key), Does.Contain(DisplayOptionsPack.Borderless).And.Contain(DisplayOptionsPack.Windowed));
                Assert.That(pack.Mode.Value, Is.EqualTo(DisplayOptionsPack.Borderless));

                var native = pack.Displays[0];
                Assert.That(pack.Resolution.Choices.Count, Is.GreaterThan(0));
                Assert.That(pack.Resolution.IndexOf(DisplayOptionsPack.ResolutionKey(native.width, native.height)), Is.GreaterThanOrEqualTo(0));
                Assert.That(DisplayOptionsPack.TryParseResolution(pack.Resolution.Value, out int width, out int height), Is.True);
                Assert.That(width, Is.GreaterThan(0));
                Assert.That(height, Is.GreaterThan(0));

                Assert.That(pack.Monitor.Choices.Count, Is.EqualTo(pack.Displays.Count));
                Assert.That(pack.FpsCap.Choices.Select(choice => choice.Key), Is.EqualTo(new[] { "Off", "30", "60", "120", "144", "240" }));
            }
        }

        [Test]
        public void AddDisplay_VisibilityRules()
        {
            using (var pack = store.AddDisplay())
            {
                Assert.That(pack.Resolution.IsVisible, Is.False, "resolution is hidden while borderless");
                pack.Mode.Value = DisplayOptionsPack.Windowed;
                Assert.That(pack.Resolution.IsVisible, Is.True);

                Assert.That(pack.FpsCap.IsEnabled, Is.False, "the cap is disabled while vsync is on");
                pack.VSync.Value = false;
                Assert.That(pack.FpsCap.IsEnabled, Is.True);

                Assert.That(pack.Monitor.IsVisible, Is.EqualTo(pack.Displays.Count > 1));
            }
        }

        [Test]
        public void AddDisplay_ConfigTrimsOptions()
        {
            var config = new DisplayOptionsPack.Config
            {
                Category = "Video",
                IncludeMonitor = false,
                IncludeFpsCap = false,
                DefaultMode = DisplayOptionsPack.Windowed,
                FpsCaps = new[] { 60 },
            };

            using (var pack = store.AddDisplay(config))
            {
                Assert.That(pack.Monitor, Is.Null);
                Assert.That(pack.FpsCap, Is.Null);
                Assert.That(pack.VSync, Is.Not.Null);
                Assert.That(store.Count, Is.EqualTo(3));
                Assert.That(pack.Mode.Value, Is.EqualTo(DisplayOptionsPack.Windowed));
                Assert.That(pack.Mode.Category, Is.EqualTo("Video"));
            }
        }

        [Test]
        public void ResolutionKeys_RoundTrip()
        {
            Assert.That(DisplayOptionsPack.ResolutionKey(1920, 1080), Is.EqualTo("1920x1080"));
            Assert.That(DisplayOptionsPack.TryParseResolution("2560x1440", out int width, out int height), Is.True);
            Assert.That((width, height), Is.EqualTo((2560, 1440)));
            Assert.That(DisplayOptionsPack.TryParseResolution("x1440", out _, out _), Is.False);
            Assert.That(DisplayOptionsPack.TryParseResolution("wide", out _, out _), Is.False);
            Assert.That(DisplayOptionsPack.TryParseResolution("", out _, out _), Is.False);
        }

        [Test]
        public void ResolveMode_MapsKeysToFullScreenModes()
        {
            Assert.That(DisplayOptionsPack.ResolveMode(DisplayOptionsPack.Borderless), Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(DisplayOptionsPack.ResolveMode(DisplayOptionsPack.Windowed), Is.EqualTo(FullScreenMode.Windowed));
            var fullscreen = DisplayOptionsPack.ResolveMode(DisplayOptionsPack.Fullscreen);
            Assert.That(fullscreen, Is.EqualTo(DisplayOptionsPack.SupportsExclusiveFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow));
            Assert.That(DisplayOptionsPack.ResolveMode("anything"), Is.EqualTo(FullScreenMode.FullScreenWindow));
        }

        [Test]
        public void AddQuality_RegistersTheProjectLevels()
        {
            using (var pack = store.AddQuality("Video"))
            {
                Assert.That(store.Get(QualityOptionsPack.QualityId), Is.SameAs(pack.Quality));
                Assert.That(pack.Quality.Choices.Select(choice => choice.Key), Is.EqualTo(QualitySettings.names));
                Assert.That(pack.Quality.Value, Is.EqualTo(QualitySettings.names[QualitySettings.GetQualityLevel()]));
                Assert.That(pack.Quality.Category, Is.EqualTo("Video"));
            }
        }
    }
}
