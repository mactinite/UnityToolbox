using System.Linq;
using NUnit.Framework;
using toolbox.Options;
using toolbox.Options.Input;
using toolbox.Options.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace toolbox.Tests
{
    public class ControlsTests
    {
        InputActionAsset asset;
        OptionsStore store;
        ControlsOptionsPack pack;

        [SetUp]
        public void SetUp()
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var ship = asset.AddActionMap("Ship");
            var fire = ship.AddAction("Fire", binding: "<Keyboard>/space", groups: "Keyboard&Mouse");
            fire.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
            var boost = ship.AddAction("Boost", binding: "<Keyboard>/leftShift", groups: "Keyboard&Mouse");
            boost.AddBinding("<Gamepad>/buttonEast", groups: "Gamepad");
            var move = ship.AddAction("Move");
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/s", groups: "Keyboard&Mouse")
                .With("Positive", "<Keyboard>/w", groups: "Keyboard&Mouse");
            var aim = ship.AddAction("Aim", binding: "<Mouse>/position", groups: "Keyboard&Mouse");
            var ui = asset.AddActionMap("UI");
            ui.AddAction("Submit", binding: "<Keyboard>/enter", groups: "Keyboard&Mouse");
            asset.AddControlScheme("Keyboard&Mouse").WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");
            asset.AddControlScheme("Gamepad").WithRequiredDevice("<Gamepad>");

            store = new OptionsStore(new InMemoryOptionsStorage());
            pack = store.AddControls(asset, new ControlsConfig { Maps = new[] { "Ship" }, ExcludedActions = new[] { "Aim" } });
            store.Load();
        }

        [TearDown]
        public void TearDown()
        {
            pack.Dispose();
            store.Dispose();
            Object.DestroyImmediate(asset);
        }

        InputAction Rebindable(string name) => pack.Asset.FindActionMap("Ship").FindAction(name);

        [Test]
        public void Pack_RegistersAHiddenKeybindOption_AndClonesTheAsset()
        {
            Assert.That(store.Get("controls.bindings"), Is.SameAs(pack.Bindings));
            Assert.That(pack.Bindings.HasFlag(OptionFlags.Hidden), Is.True);
            Assert.That(pack.Bindings.Presentation, Is.EqualTo(OptionPresentation.Keybind));
            Assert.That(pack.Asset, Is.Not.SameAs(asset));
            Assert.That(pack.Bindings.IsDefault, Is.True);
        }

        [Test]
        public void Schemes_AreThoseWithBindingsInTheChosenMaps()
        {
            Assert.That(pack.Schemes().ToList(), Is.EqualTo(new[] { "Keyboard&Mouse", "Gamepad" }));
        }

        [Test]
        public void Targets_OnePerBinding_CompositePartsIncluded_ExcludedAndOtherMapsSkipped()
        {
            var keyboard = pack.Targets("Keyboard&Mouse").Select(target => target.Label).ToList();
            var gamepad = pack.Targets("Gamepad").Select(target => target.Label).ToList();

            Assert.That(keyboard, Is.EqualTo(new[] { "Fire", "Boost", "Move · Negative", "Move · Positive" }));
            Assert.That(gamepad, Is.EqualTo(new[] { "Fire", "Boost" }));
            Assert.That(pack.Targets("Keyboard&Mouse").First().DisplayString(), Is.EqualTo("Space"));
        }

        [Test]
        public void AddToPage_PlacesOneRowPerTarget_UnderASectionPerScheme()
        {
            var layout = new OptionsLayout();
            pack.AddToPage(layout.Page("controls", "Controls"));

            var page = layout.Pages[0];
            Assert.That(page.Sections.Select(section => section.Label).ToList(), Is.EqualTo(new[] { "Keyboard&Mouse", "Gamepad" }));
            Assert.That(page.OptionEntries.Count(), Is.EqualTo(6));
            Assert.That(page.OptionEntries.All(entry => entry.OptionId == "controls.bindings" && entry.Tag is KeybindTarget && entry.RowFactory != null), Is.True);
            Assert.That(layout.DuplicateIds(), Is.Empty, "tagged entries may share the option");
        }

        [Test]
        public void CaptureAndApply_RoundTrip_ThroughAnotherCopy()
        {
            Rebindable("Fire").ApplyBindingOverride(0, "<Keyboard>/f");
            pack.Bindings.Capture(pack.Asset);
            Assert.That(pack.Bindings.Value, Does.Contain("<Keyboard>/f"));
            Assert.That(pack.Bindings.IsDefault, Is.False);

            var live = Object.Instantiate(asset);
            try
            {
                using (pack.Track(live))
                {
                    Assert.That(live.FindAction("Ship/Fire").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/f"));
                    pack.ResetAll();
                    Assert.That(pack.Bindings.Value, Is.EqualTo(""));
                    Assert.That(live.FindAction("Ship/Fire").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
                }
            }
            finally
            {
                Object.DestroyImmediate(live);
            }
        }

        [Test]
        public void Capture_WithoutOverrides_IsEmpty()
        {
            pack.Bindings.Capture(pack.Asset);
            Assert.That(pack.Bindings.Value, Is.EqualTo(""));
        }

        [Test]
        public void StoreRoundTrip_RestoresBindingsOnTheRebindingCopy()
        {
            Rebindable("Boost").ApplyBindingOverride(0, "<Keyboard>/b");
            pack.Bindings.Capture(pack.Asset);
            store.Save();

            pack.Bindings.Value = "";
            Assert.That(Rebindable("Boost").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/leftShift"), "clearing the option clears the rebinding copy");
            store.Load();

            Assert.That(Rebindable("Boost").bindings[0].effectivePath, Is.EqualTo("<Keyboard>/b"));
        }

        [Test]
        public void ResolveDuplicates_Swap_GivesTheOtherActionTheOldKey()
        {
            var fire = Rebindable("Fire");
            var boost = Rebindable("Boost");
            fire.ApplyBindingOverride(0, "<Keyboard>/leftShift"); // Boost's key

            var outcome = RebindFlow.ResolveDuplicates(fire, 0, "<Keyboard>/space", DuplicatePolicy.Swap);

            Assert.That(outcome, Is.EqualTo(RebindOutcome.Completed));
            Assert.That(fire.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/leftShift"));
            Assert.That(boost.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
        }

        [Test]
        public void ResolveDuplicates_Block_RestoresTheOldKey()
        {
            var fire = Rebindable("Fire");
            fire.ApplyBindingOverride(0, "<Keyboard>/leftShift");

            var outcome = RebindFlow.ResolveDuplicates(fire, 0, "<Keyboard>/space", DuplicatePolicy.Block);

            Assert.That(outcome, Is.EqualTo(RebindOutcome.Blocked));
            Assert.That(fire.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/space"));
            Assert.That(fire.bindings[0].hasOverrides, Is.False);
        }

        [Test]
        public void ResolveDuplicates_IgnoresOtherSchemes_AndAllowPolicy()
        {
            var fire = Rebindable("Fire");
            fire.ApplyBindingOverride(1, "<Gamepad>/buttonEast"); // Boost's gamepad key, same scheme
            Assert.That(RebindFlow.ResolveDuplicates(fire, 1, "<Gamepad>/buttonSouth", DuplicatePolicy.Allow), Is.EqualTo(RebindOutcome.Completed));
            Assert.That(Rebindable("Boost").bindings[1].effectivePath, Is.EqualTo("<Gamepad>/buttonEast"), "Allow keeps both");

            fire.ApplyBindingOverride(0, "<Gamepad>/buttonEast"); // a keyboard-scheme slot: not a duplicate of the gamepad binding
            Assert.That(RebindFlow.ResolveDuplicates(fire, 0, "<Keyboard>/space", DuplicatePolicy.Block), Is.EqualTo(RebindOutcome.Completed));
        }

        [Test]
        public void SchemeDevicePaths_ComeFromTheBindingGroup()
        {
            var fire = Rebindable("Fire");
            Assert.That(RebindFlow.SchemeDevicePaths(fire, 0).ToList(), Is.EqualTo(new[] { "<Keyboard>", "<Mouse>" }));
            Assert.That(RebindFlow.SchemeDevicePaths(fire, 1).ToList(), Is.EqualTo(new[] { "<Gamepad>" }));
        }
    }
}
