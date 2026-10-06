using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>A uGUI <see cref="Toggle"/>. Right turns the option on, left off; submit flips it.</summary>
    public class ToggleRow : OptionRow
    {
        [SerializeField] Toggle toggle;

        protected override void OnBind()
        {
            if (toggle == null)
                toggle = primary as Toggle;
            if (toggle == null)
            {
                Debug.LogWarning($"[Options] ToggleRow for '{Option.Id}' has no Toggle.", this);
                return;
            }

            toggle.onValueChanged.RemoveListener(OnToggled);
            toggle.onValueChanged.AddListener(OnToggled);
        }

        protected override void OnRefresh()
        {
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(Current());
        }

        bool Current() => Context.GetValue(Option) is bool on && on;

        void OnToggled(bool on)
        {
            if (on != Current())
                Write(on);
        }

        public override void Adjust(int direction) => Write(direction > 0);

        protected override string ValueLabel()
        {
            bool on = Current();
            return Option is BoolOption boolOption ? (on ? boolOption.OnLabel : boolOption.OffLabel) : on.ToString();
        }

        internal void Configure(Toggle widget) => toggle = widget;
    }

    /// <summary>
    /// A uGUI <see cref="Slider"/> over an <see cref="IRangeOption"/>. The value is written live while dragging and
    /// committed on release; keyboard and gamepad moves commit at once.
    /// </summary>
    public class SliderRow : OptionRow
    {
        [SerializeField] Slider slider;
        [Tooltip("Commit when the pointer is released rather than on every change.")]
        [SerializeField] bool commitOnRelease = true;

        IRangeOption range;

        public override bool HandlesHorizontalMove => false;

        protected override void OnBind()
        {
            if (slider == null)
                slider = primary as Slider;
            range = Option as IRangeOption;
            if (slider == null || range == null)
            {
                Debug.LogWarning($"[Options] SliderRow for '{Option.Id}' needs a Slider and a numeric option.", this);
                return;
            }

            slider.minValue = range.MinValue;
            slider.maxValue = range.MaxValue;
            slider.wholeNumbers = range.IsInteger;
            slider.onValueChanged.RemoveListener(OnSliderChanged);
            slider.onValueChanged.AddListener(OnSliderChanged);

            var release = slider.GetComponent<SliderReleaseRelay>();
            if (release == null)
                release = slider.gameObject.AddComponent<SliderReleaseRelay>();
            release.Released = OnReleased;
        }

        protected override void OnRefresh()
        {
            if (slider != null && range != null)
                slider.SetValueWithoutNotify(Current());
        }

        float Current() => Convert.ToSingle(Context.GetValue(Option), CultureInfo.InvariantCulture);

        void OnSliderChanged(float value)
        {
            if (Mathf.Approximately(value, Current()))
                return;
            Write(range.IsInteger ? (object)Mathf.RoundToInt(value) : value, commit: !commitOnRelease);
        }

        void OnReleased()
        {
            if (commitOnRelease)
                CommitOnly();
        }

        internal override void OnHorizontal(int direction)
        {
            // The Slider already moved itself; make the keyboard step final.
            if (commitOnRelease)
                CommitOnly();
        }

        internal void Configure(Slider widget) => slider = widget;
    }

    /// <summary>Reports pointer release and drag end so a slider can commit once.</summary>
    public sealed class SliderReleaseRelay : MonoBehaviour, IPointerUpHandler, IEndDragHandler
    {
        public Action Released { get; set; }

        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();

        public void OnEndDrag(PointerEventData eventData) => Released?.Invoke();
    }

    /// <summary>
    /// Left/right arrows around a value. Steps through an <see cref="IChoiceOption"/>'s choices, or an
    /// <see cref="IRangeOption"/> by its step. Gamepad friendly: left/right on the row changes the value, submit cycles.
    /// </summary>
    public class StepperRow : OptionRow
    {
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [Tooltip("Wrap around the ends (choice options may also set Wrap themselves).")]
        [SerializeField] bool wrap;

        IChoiceOption choices;
        IRangeOption range;

        protected override void OnBind()
        {
            choices = Option as IChoiceOption;
            range = Option as IRangeOption;
            if (choices == null && range == null)
                Debug.LogWarning($"[Options] StepperRow for '{Option.Id}' needs a choice or numeric option.", this);

            Hook(previousButton, -1);
            Hook(nextButton, 1);
        }

        void Hook(Button button, int direction)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Adjust(direction));
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        protected override void OnRefresh()
        {
            bool enabled = Option.IsEnabled;
            if (previousButton != null)
                previousButton.interactable = enabled && CanStep(-1);
            if (nextButton != null)
                nextButton.interactable = enabled && CanStep(1);
        }

        bool Wraps => wrap || (choices != null && choices.Wrap);

        int CurrentIndex()
        {
            object current = Context.GetValue(Option);
            string key = current as string ?? current?.ToString();
            var list = choices.Choices;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                    return i;
            }

            return -1;
        }

        bool CanStep(int direction)
        {
            if (choices != null)
            {
                int count = choices.Choices.Count;
                if (count == 0)
                    return false;
                if (Wraps)
                    return count > 1;
                int index = CurrentIndex();
                return direction > 0 ? index < count - 1 : index > 0;
            }

            if (range != null)
            {
                float current = Convert.ToSingle(Context.GetValue(Option), CultureInfo.InvariantCulture);
                return direction > 0 ? current < range.MaxValue : current > range.MinValue;
            }

            return false;
        }

        public override void Adjust(int direction)
        {
            if (!CanStep(direction))
            {
                Deny();
                return;
            }

            if (choices != null)
            {
                int count = choices.Choices.Count;
                int index = CurrentIndex() + direction;
                index = Wraps ? ((index % count) + count) % count : Mathf.Clamp(index, 0, count - 1);
                Write(choices.Choices[index].Key);
                return;
            }

            float step = range.StepValue > 0f ? range.StepValue : (range.MaxValue - range.MinValue) / 20f;
            if (range.IsInteger)
                step = Mathf.Max(1f, Mathf.Round(step));
            float current = Convert.ToSingle(Context.GetValue(Option), CultureInfo.InvariantCulture);
            float next = Mathf.Clamp(current + direction * step, range.MinValue, range.MaxValue);
            Write(range.IsInteger ? (object)Mathf.RoundToInt(next) : next);
        }

        public override void Activate()
        {
            if (choices == null)
                return;
            int count = choices.Choices.Count;
            if (count < 2)
                return;
            int index = (CurrentIndex() + 1) % count;
            Write(choices.Choices[index].Key);
        }

        protected override string ValueLabel()
        {
            if (choices != null)
            {
                int index = CurrentIndex();
                if (index >= 0)
                    return Context.Text.ChoiceLabel(Option, choices.Choices[index]);
            }

            return base.ValueLabel();
        }

        internal void Configure(Button previous, Button next)
        {
            previousButton = previous;
            nextButton = next;
        }
    }

    /// <summary>A <see cref="TMP_Dropdown"/> over an <see cref="IChoiceOption"/>.</summary>
    public class DropdownRow : OptionRow
    {
        [SerializeField] TMP_Dropdown dropdown;

        IChoiceOption choices;

        protected override void OnBind()
        {
            if (dropdown == null)
                dropdown = primary as TMP_Dropdown;
            choices = Option as IChoiceOption;
            if (dropdown == null || choices == null)
            {
                Debug.LogWarning($"[Options] DropdownRow for '{Option.Id}' needs a TMP_Dropdown and a choice option.", this);
                return;
            }

            dropdown.onValueChanged.RemoveListener(OnPicked);
            dropdown.onValueChanged.AddListener(OnPicked);
            RebuildOptions();
        }

        void RebuildOptions()
        {
            dropdown.ClearOptions();
            var labels = new List<string>(choices.Choices.Count);
            foreach (var choice in choices.Choices)
                labels.Add(Context.Text.ChoiceLabel(Option, choice));
            dropdown.AddOptions(labels);
        }

        protected override void OnRefresh()
        {
            if (dropdown == null || choices == null)
                return;
            if (dropdown.options.Count != choices.Choices.Count)
                RebuildOptions();
            dropdown.SetValueWithoutNotify(Mathf.Max(0, CurrentIndex()));
        }

        int CurrentIndex()
        {
            object current = Context.GetValue(Option);
            string key = current as string ?? current?.ToString();
            var list = choices.Choices;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                    return i;
            }

            return -1;
        }

        void OnPicked(int index)
        {
            if (index >= 0 && index < choices.Choices.Count && index != CurrentIndex())
                Write(choices.Choices[index].Key);
        }

        public override void Adjust(int direction)
        {
            int count = choices.Choices.Count;
            int index = Mathf.Clamp(CurrentIndex() + direction, 0, count - 1);
            if (index == CurrentIndex())
                Deny();
            else
                Write(choices.Choices[index].Key);
        }

        public override void Activate() => dropdown?.Show();

        internal void Configure(TMP_Dropdown widget) => dropdown = widget;
    }

    /// <summary>A <see cref="TMP_InputField"/>. Text is validated through the option before it is written.</summary>
    public class TextRow : OptionRow
    {
        [SerializeField] TMP_InputField input;

        public override bool HandlesHorizontalMove => false;

        protected override void OnBind()
        {
            if (input == null)
                input = primary as TMP_InputField;
            if (input == null)
            {
                Debug.LogWarning($"[Options] TextRow for '{Option.Id}' has no TMP_InputField.", this);
                return;
            }

            if (Option is StringOption text && text.MaxLength > 0)
                input.characterLimit = text.MaxLength;
            input.onEndEdit.RemoveListener(OnEndEdit);
            input.onEndEdit.AddListener(OnEndEdit);
        }

        protected override void OnRefresh()
        {
            if (input != null && !input.isFocused)
                input.SetTextWithoutNotify(CurrentText());
        }

        string CurrentText()
        {
            object current = Context.GetValue(Option);
            return current is string text ? text : Option.Serialize();
        }

        void OnEndEdit(string text)
        {
            if (text == CurrentText())
                return;
            if (Option.TryParseValue(text, out var value))
            {
                Write(value);
            }
            else
            {
                Deny();
                Refresh();
            }
        }

        public override void Activate() => input?.ActivateInputField();

        internal void Configure(TMP_InputField widget) => input = widget;
    }
}
