using System;
using NUnit.Framework;
using toolbox.Options;

namespace toolbox.Tests
{
    public class OptionTests
    {
        enum Speed
        {
            Slow,
            Normal,
            Fast,
        }

        [Test]
        public void Label_DefaultsToHumanizedLastSegment_AndCategoryToFirst()
        {
            var option = new BoolOption("gameplay.screenShake");

            Assert.That(option.Label, Is.EqualTo("Screen Shake"));
            Assert.That(option.Category, Is.EqualTo("Gameplay"));
        }

        [Test]
        public void Category_DefaultsToGeneral_WithoutDot()
        {
            Assert.That(new BoolOption("mute").Category, Is.EqualTo("General"));
        }

        [Test]
        public void Humanize_SplitsWordsAndCapitalizes()
        {
            Assert.That(Option.Humanize("muteInBackground"), Is.EqualTo("Mute In Background"));
            Assert.That(Option.Humanize("fps_cap"), Is.EqualTo("Fps Cap"));
            Assert.That(Option.Humanize("VSync"), Is.EqualTo("VSync"));
            Assert.That(Option.Humanize("hd2dTilt"), Is.EqualTo("Hd2d Tilt"));
        }

        [Test]
        public void EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BoolOption(""));
            Assert.Throws<ArgumentException>(() => new BoolOption("  "));
        }

        [Test]
        public void Set_RaisesChangedOnce_AndOnlyWhenDifferent()
        {
            var option = new BoolOption("a.b");
            int changed = 0, typed = 0;
            option.Changed += () => changed++;
            option.ValueChanged += _ => typed++;

            Assert.That(option.Set(true), Is.True);
            Assert.That(option.Set(true), Is.False);

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(typed, Is.EqualTo(1));
        }

        [Test]
        public void Reset_RestoresDefault()
        {
            var option = new IntOption("a.b", 3);
            option.Value = 9;
            Assert.That(option.IsDefault, Is.False);

            option.Reset();

            Assert.That(option.IsDefault, Is.True);
            Assert.That(option.Value, Is.EqualTo(3));
        }

        [Test]
        public void FloatOption_ClampsAndSnaps()
        {
            var option = new FloatOption("a.v", 0.5f, 0f, 1f, 0.25f);

            option.Value = 0.6f;
            Assert.That(option.Value, Is.EqualTo(0.5f).Within(1e-5f));
            option.Value = 7f;
            Assert.That(option.Value, Is.EqualTo(1f));
            option.Value = -1f;
            Assert.That(option.Value, Is.EqualTo(0f));
            option.Value = float.NaN;
            Assert.That(option.Value, Is.EqualTo(0.5f));
        }

        [Test]
        public void FloatOption_RejectsDefaultOutsideRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FloatOption("a.v", 2f, 0f, 1f));
            Assert.Throws<ArgumentException>(() => new FloatOption("a.v", 0f, 1f, 0f));
        }

        [Test]
        public void FloatOption_FormatsPercentOfRange()
        {
            var volume = new FloatOption("audio.master", 0.8f);
            Assert.That(volume.DisplayValue, Is.EqualTo("0.8"));

            volume.Format = OptionFormat.Percent;
            Assert.That(volume.DisplayValue, Is.EqualTo("80%"));

            var wide = new FloatOption("a.b", 50f, 0f, 200f) { Format = OptionFormat.Percent };
            Assert.That(wide.DisplayValue, Is.EqualTo("25%"));

            var seconds = new FloatOption("a.s", 1.5f, 0f, 10f) { Format = OptionFormat.OneDecimal, Suffix = " s" };
            Assert.That(seconds.DisplayValue, Is.EqualTo("1.5 s"));

            var custom = new FloatOption("a.c", 0.25f) { Formatter = v => $"[{v}]" };
            Assert.That(custom.DisplayValue, Is.EqualTo("[0.25]"));
        }

        [Test]
        public void FloatOption_SerializesInvariantCulture()
        {
            var option = new FloatOption("a.b", 0.5f);
            option.Value = 0.25f;

            Assert.That(option.Serialize(), Is.EqualTo("0.25"));
            Assert.That(option.TrySetFromString("0.75"), Is.True);
            Assert.That(option.Value, Is.EqualTo(0.75f));
            Assert.That(option.TrySetFromString("abc"), Is.False);
            Assert.That(option.TrySetFromString("NaN"), Is.False);
            Assert.That(option.Value, Is.EqualTo(0.75f));
        }

        [Test]
        public void IntOption_ClampsAndSteps()
        {
            var option = new IntOption("a.b", 10, 0, 100, 10);

            option.Value = 14;
            Assert.That(option.Value, Is.EqualTo(10));
            option.Value = 16;
            Assert.That(option.Value, Is.EqualTo(20));
            option.Value = 500;
            Assert.That(option.Value, Is.EqualTo(100));
            Assert.That(option.TrySetFromString("30"), Is.True);
            Assert.That(option.Value, Is.EqualTo(30));
            Assert.That(option.TrySetFromString("3.5"), Is.False);
        }

        [Test]
        public void IntOption_AsRangeOption()
        {
            IRangeOption range = new IntOption("a.b", 5, 0, 10);

            Assert.That(range.MinValue, Is.EqualTo(0f));
            Assert.That(range.MaxValue, Is.EqualTo(10f));
            Assert.That(range.IsInteger, Is.True);
            range.FloatValue = 7.6f;
            Assert.That(range.FloatValue, Is.EqualTo(8f));
        }

        [Test]
        public void BoolOption_ParsesCommonSpellings()
        {
            var option = new BoolOption("a.b");

            Assert.That(option.TrySetFromString("On"), Is.True);
            Assert.That(option.Value, Is.True);
            Assert.That(option.TrySetFromString("0"), Is.True);
            Assert.That(option.Value, Is.False);
            Assert.That(option.TrySetFromString("maybe"), Is.False);
            Assert.That(option.Serialize(), Is.EqualTo("false"));
            Assert.That(option.DisplayValue, Is.EqualTo("Off"));
        }

        [Test]
        public void ChoiceOption_FallsBackToDefault_ForUnknownKey()
        {
            var option = new ChoiceOption("d.mode", "Borderless", "Borderless", "Windowed");

            option.Value = "Exclusive";
            Assert.That(option.Value, Is.EqualTo("Borderless"));

            Assert.That(option.TrySetFromString("Windowed"), Is.True);
            Assert.That(option.Value, Is.EqualTo("Windowed"));

            Assert.That(option.TrySetFromString("Nope"), Is.True, "an unknown key is accepted and coerced");
            Assert.That(option.Value, Is.EqualTo("Borderless"));
        }

        [Test]
        public void ChoiceOption_SetChoicesRevalidates()
        {
            var option = new ChoiceOption("d.res", "1920x1080", "1280x720", "1920x1080");
            option.Value = "1280x720";

            option.SetChoices(new[] { new Choice("2560x1440"), new Choice("1920x1080") });
            Assert.That(option.Value, Is.EqualTo("1920x1080"), "falls back to the default when it exists");

            option.SetChoices(new[] { new Choice("800x600", "800 x 600") });
            Assert.That(option.Value, Is.EqualTo("800x600"), "else to the first choice");
            Assert.That(option.DisplayValue, Is.EqualTo("800 x 600"));
        }

        [Test]
        public void ChoiceOption_StepsWithAndWithoutWrap()
        {
            var option = new ChoiceOption("a.b", "x", "x", "y", "z");

            option.Next();
            option.Next();
            option.Next();
            Assert.That(option.Value, Is.EqualTo("z"));

            option.Wrap = true;
            option.Next();
            Assert.That(option.Value, Is.EqualTo("x"));
            option.Previous();
            Assert.That(option.Value, Is.EqualTo("z"));
            Assert.That(option.SelectedIndex, Is.EqualTo(2));
            Assert.That(option.SelectedLabel, Is.EqualTo("z"));

            option.SelectedIndex = 99;
            Assert.That(option.Value, Is.EqualTo("z"));
        }

        [Test]
        public void ChoiceOption_WithoutChoicesKeepsAnyValue()
        {
            var option = new ChoiceOption("a.b", "anything", Array.Empty<Choice>());
            option.Value = "else";
            Assert.That(option.Value, Is.EqualTo("else"));
            Assert.That(option.SelectedIndex, Is.EqualTo(-1));
        }

        [Test]
        public void EnumOption_StoresNames_AndParsesCaseInsensitively()
        {
            var option = new EnumOption<Speed>("g.textSpeed", Speed.Normal);

            Assert.That(option.Serialize(), Is.EqualTo("Normal"));
            Assert.That(option.TrySetFromString("fast"), Is.True);
            Assert.That(option.Value, Is.EqualTo(Speed.Fast));
            Assert.That(option.TrySetFromString("Warp"), Is.False);
            Assert.That(option.Choices.Count, Is.EqualTo(3));
            Assert.That(option.SelectedIndex, Is.EqualTo(2));
            Assert.That(option.DisplayValue, Is.EqualTo("Fast"));
        }

        [Test]
        public void EnumOption_RestrictsToAllowedValues_AndLabels()
        {
            var option = new EnumOption<Speed>("g.s", Speed.Slow, Speed.Slow, Speed.Fast).WithLabels(s => s.ToString().ToUpperInvariant());

            option.Value = Speed.Normal;
            Assert.That(option.Value, Is.EqualTo(Speed.Slow));
            Assert.That(option.Choices.Count, Is.EqualTo(2));
            Assert.That(option.Choices[1].Label, Is.EqualTo("FAST"));

            option.Next();
            Assert.That(option.Value, Is.EqualTo(Speed.Fast));
            option.Next();
            Assert.That(option.Value, Is.EqualTo(Speed.Fast));
        }

        [Test]
        public void StringOption_TruncatesToMaxLength_AndNeverNull()
        {
            var option = new StringOption("a.s") { MaxLength = 3 };

            option.Value = "abcdef";
            Assert.That(option.Value, Is.EqualTo("abc"));
            option.Value = null;
            Assert.That(option.Value, Is.EqualTo(""));
            Assert.That(option.Serialize(), Is.EqualTo(""));
        }

        [Test]
        public void BoxedValue_ConvertsCompatibleTypes()
        {
            var number = new FloatOption("a.f", 0f, 0f, 10f);
            number.BoxedValue = 3;
            Assert.That(number.Value, Is.EqualTo(3f));
            number.BoxedValue = "4.5";
            Assert.That(number.Value, Is.EqualTo(4.5f));

            var speed = new EnumOption<Speed>("a.e", Speed.Slow);
            speed.BoxedValue = "Fast";
            Assert.That(speed.Value, Is.EqualTo(Speed.Fast));
            speed.BoxedValue = 1;
            Assert.That(speed.Value, Is.EqualTo(Speed.Normal));

            Assert.That(number.BoxedDefault, Is.EqualTo(0f));
            Assert.That(number.ValueType, Is.EqualTo(typeof(float)));
        }

        [Test]
        public void Bind_AppliesImmediatelyWithoutStore_AndOnEveryChange_UntilDisposed()
        {
            var option = new FloatOption("a.b", 0.5f);
            float seen = -1f;
            int calls = 0;

            using (option.Bind(v => { seen = v; calls++; }))
            {
                Assert.That(seen, Is.EqualTo(0.5f));
                option.Value = 0.9f;
                Assert.That(seen, Is.EqualTo(0.9f));
                Assert.That(calls, Is.EqualTo(2));
            }

            option.Value = 0.1f;
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void VisibilityAndEnabledPredicates()
        {
            var vsync = new BoolOption("d.vsync", true);
            var cap = new ChoiceOption("d.cap", "Off", "Off", "60") { EnabledWhen = () => !vsync.Value };
            Assert.That(cap.IsEnabled, Is.False);
            vsync.Value = false;
            Assert.That(cap.IsEnabled, Is.True);

            Assert.That(new BoolOption("a.h") { Flags = OptionFlags.Hidden }.IsVisible, Is.False);
            Assert.That(new BoolOption("a.c") { VisibleWhen = () => false }.IsVisible, Is.False);
            Assert.That(new BoolOption("a.u") { AvailableWhen = () => false }.IsVisible, Is.False);
            Assert.That(new BoolOption("a.v").IsVisible, Is.True);
            Assert.That(new BoolOption("a.f") { Flags = OptionFlags.Confirm | OptionFlags.DevOnly }.HasFlag(OptionFlags.DevOnly), Is.True);
        }
    }
}
