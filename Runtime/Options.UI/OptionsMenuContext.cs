using System;

namespace toolbox.Options.UI
{
    /// <summary>
    /// What rows need from the menu that hosts them: the store, the transaction that stages <see cref="OptionFlags.Confirm"/>
    /// options, the text provider, feedback, and the write path. Rows never touch the store directly.
    /// </summary>
    public sealed class OptionsMenuContext
    {
        public OptionsMenuContext(OptionsStore store, OptionsTransaction transaction = null, IOptionTextProvider text = null,
            IOptionsMenuFeedback feedback = null, Func<bool> showDevOnly = null)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Transaction = transaction;
            Text = text ?? new DefaultOptionTextProvider();
            Feedback = feedback;
            ShowDevOnly = showDevOnly ?? DefaultDevGate;
        }

        public OptionsStore Store { get; }
        public OptionsTransaction Transaction { get; }
        public IOptionTextProvider Text { get; }
        public IOptionsMenuFeedback Feedback { get; set; }
        public Func<bool> ShowDevOnly { get; }

        /// <summary>A row's primary widget was selected.</summary>
        public event Action<OptionRow> RowSelected;

        /// <summary>A non-staged value was committed; the host saves per its policy.</summary>
        public event Action<Option> ValueCommitted;

        /// <summary>A staged value changed; the host shows or hides its Apply button.</summary>
        public event Action<Option> StagedChanged;

        /// <summary>A row received Cancel.</summary>
        public event Action CloseRequested;

        /// <summary>Whether the option's edits go through the transaction instead of the option itself.</summary>
        public bool IsStaged(Option option) =>
            option.HasFlag(OptionFlags.Confirm) && Transaction != null && Transaction.IsTracking(option);

        /// <summary>The value a row should show: the staged one when there is one, else the committed one.</summary>
        public object GetValue(Option option) => IsStaged(option) ? Transaction.Get(option) : option.BoxedValue;

        public bool IsPending(Option option) => IsStaged(option) && Transaction.IsPending(option);

        public bool ShowsDefault(Option option) => Equals(GetValue(option), option.BoxedDefault);

        /// <summary>
        /// Whether a built row should be active: unavailable and dev-only options stay out, and
        /// <see cref="Option.VisibleWhen"/> is re-evaluated as values change. The Hidden flag is not consulted here:
        /// it decides whether an option gets a row of its own at all (see the builder), and explicitly placed rows
        /// such as key bindings share a hidden option.
        /// </summary>
        public bool ShouldShow(Option option) =>
            option.IsAvailable
            && (option.VisibleWhen == null || option.VisibleWhen())
            && (!option.HasFlag(OptionFlags.DevOnly) || ShowDevOnly());

        /// <summary>Writes a value from a row: staged options go to the transaction, others straight to the option.</summary>
        public void SetValue(OptionRow row, object value, bool commit)
        {
            var option = row.Option;
            if (IsStaged(option))
            {
                Transaction.Set(option, value);
                StagedChanged?.Invoke(option);
            }
            else
            {
                option.BoxedValue = value;
            }

            Feedback?.OnValueChanged(row);
            if (commit)
                Commit(row);
        }

        /// <summary>Marks a live edit as final (a slider released). Staged options commit through the host's Apply.</summary>
        public void Commit(OptionRow row)
        {
            if (IsStaged(row.Option))
                return;
            ValueCommitted?.Invoke(row.Option);
            Feedback?.OnValueCommitted(row);
        }

        public void RequestClose() => CloseRequested?.Invoke();

        internal void NotifySelected(OptionRow row)
        {
            RowSelected?.Invoke(row);
            Feedback?.OnRowSelected(row);
        }

        static bool DefaultDevGate() => UnityEngine.Debug.isDebugBuild;
    }
}
