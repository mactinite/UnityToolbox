using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace toolbox.Options.UI
{
    /// <summary>
    /// One option in a menu. Widgets are wired through serialized fields, so a prefab can be laid out any way;
    /// subclass for a new widget or to decorate an existing one. The row writes through its
    /// <see cref="OptionsMenuContext"/>, which stages <see cref="OptionFlags.Confirm"/> options.
    /// </summary>
    public abstract class OptionRow : MonoBehaviour
    {
        [SerializeField] protected TMP_Text label;
        [SerializeField] protected TMP_Text valueText;
        [SerializeField] protected Button resetButton;
        [Tooltip("The selectable that receives focus and navigation for this row.")]
        [SerializeField] protected Selectable primary;
        [Tooltip("Tinted while the row is selected.")]
        [SerializeField] protected Graphic background;
        [SerializeField] protected GameObject pendingMarker;
        [SerializeField] protected GameObject restartMarker;

        Color backgroundColor;
        bool hasBackgroundColor;

        public Option Option { get; private set; }
        public OptionEntry Entry { get; private set; }
        public OptionsMenuContext Context { get; private set; }
        public Selectable Primary => primary;
        public bool IsSelected { get; private set; }

        /// <summary>False for widgets that use left/right themselves (sliders, text fields).</summary>
        public virtual bool HandlesHorizontalMove => true;

        public void Bind(Option option, OptionEntry entry, OptionsMenuContext context)
        {
            Option = option;
            Entry = entry;
            Context = context;

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetToDefault);
                resetButton.onClick.AddListener(ResetToDefault);
                var resetNavigation = resetButton.navigation;
                resetNavigation.mode = Navigation.Mode.None;
                resetButton.navigation = resetNavigation;
            }

            if (primary != null)
            {
                var relay = primary.GetComponent<RowInputRelay>();
                if (relay == null)
                    relay = primary.gameObject.AddComponent<RowInputRelay>();
                relay.Row = this;
            }

            if (background != null && !hasBackgroundColor)
            {
                backgroundColor = background.color;
                hasBackgroundColor = true;
            }

            OnBind();
            Refresh();
        }

        /// <summary>Re-reads the value, default state, pending state and enabled state.</summary>
        public virtual void Refresh()
        {
            if (Option == null || Context == null)
                return;
            if (label != null)
                label.text = Context.Text.Label(Option, Entry);
            if (valueText != null)
                valueText.text = ValueLabel();
            if (resetButton != null)
                resetButton.gameObject.SetActive(!Context.ShowsDefault(Option));
            if (pendingMarker != null)
                pendingMarker.SetActive(Context.IsPending(Option));
            if (restartMarker != null)
                restartMarker.SetActive(Option.HasFlag(OptionFlags.RequiresRestart));
            SetInteractable(Option.IsEnabled);
            OnRefresh();
        }

        public virtual void SetInteractable(bool interactable)
        {
            if (primary != null)
                primary.interactable = interactable;
            if (resetButton != null)
                resetButton.interactable = interactable;
            if (label != null)
                label.alpha = interactable ? 1f : 0.45f;
            if (valueText != null)
                valueText.alpha = interactable ? 1f : 0.45f;
        }

        /// <summary>Left/right from the keyboard or a gamepad, or the stepper arrows.</summary>
        public virtual void Adjust(int direction)
        {
        }

        /// <summary>Submit on the row.</summary>
        public virtual void Activate()
        {
        }

        public void ResetToDefault()
        {
            if (Option == null)
                return;
            Write(Option.BoxedDefault);
        }

        public void Select()
        {
            if (primary != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }

        /// <summary>Called once after binding, with the option available.</summary>
        protected abstract void OnBind();

        /// <summary>Called at the end of every <see cref="Refresh"/>.</summary>
        protected virtual void OnRefresh()
        {
        }

        /// <summary>The text for <c>valueText</c>; the staged value when there is one.</summary>
        protected virtual string ValueLabel()
        {
            if (!Context.IsStaged(Option))
                return Context.Text.Value(Option);
            object staged = Context.GetValue(Option);
            if (Option is IChoiceOption choices)
            {
                string key = staged as string ?? staged?.ToString();
                foreach (var choice in choices.Choices)
                {
                    if (choice.Key == key)
                        return Context.Text.ChoiceLabel(Option, choice);
                }
            }

            return staged?.ToString() ?? "";
        }

        /// <summary>Writes through the context and refreshes.</summary>
        protected void Write(object value, bool commit = true)
        {
            Context.SetValue(this, value, commit);
            Refresh();
        }

        protected void CommitOnly()
        {
            Context.Commit(this);
            Refresh();
        }

        protected void Deny() => Context.Feedback?.OnDenied(this);

        /// <summary>Horizontal input arrived while this row was selected. The default adjusts the value.</summary>
        internal virtual void OnHorizontal(int direction)
        {
            if (HandlesHorizontalMove)
                Adjust(direction);
        }

        internal void NotifySelected(bool selected)
        {
            IsSelected = selected;
            OnSelectionChanged(selected);
            if (selected)
                Context?.NotifySelected(this);
        }

        protected virtual void OnSelectionChanged(bool selected)
        {
            if (background != null && hasBackgroundColor)
                background.color = selected ? Color.Lerp(backgroundColor, Color.white, 0.12f) : backgroundColor;
        }

        internal void ConfigureBase(TMP_Text labelText, TMP_Text value, Button reset, Selectable primarySelectable, Graphic rowBackground)
        {
            label = labelText;
            valueText = value;
            resetButton = reset;
            primary = primarySelectable;
            background = rowBackground;
        }
    }

    /// <summary>
    /// Sits on a row's primary selectable and relays focus, horizontal moves, submit and cancel to the row.
    /// <c>ExecuteEvents</c> calls every handler on the object, so the selectable keeps its own behaviour.
    /// </summary>
    public sealed class RowInputRelay : MonoBehaviour, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
    {
        public OptionRow Row { get; set; }

        public void OnSelect(BaseEventData eventData) => Row?.NotifySelected(true);

        public void OnDeselect(BaseEventData eventData) => Row?.NotifySelected(false);

        public void OnMove(AxisEventData eventData)
        {
            if (Row == null)
                return;
            if (eventData.moveDir == MoveDirection.Left)
                Row.OnHorizontal(-1);
            else if (eventData.moveDir == MoveDirection.Right)
                Row.OnHorizontal(1);
        }

        public void OnSubmit(BaseEventData eventData) => Row?.Activate();

        public void OnCancel(BaseEventData eventData) => Row?.Context?.RequestClose();
    }
}
