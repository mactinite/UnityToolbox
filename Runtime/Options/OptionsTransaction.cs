using System;
using System.Collections.Generic;
using System.Linq;

namespace toolbox.Options
{
    /// <summary>
    /// Staged edits for options that must be applied together and confirmed, such as display mode and resolution.
    /// <see cref="Set{T}"/> stages, <see cref="Apply"/> writes the values (appliers react), then either
    /// <see cref="Commit"/> saves and makes them the new baseline or <see cref="Revert"/> writes the baseline back
    /// (appliers react again, undoing the change).
    /// </summary>
    public sealed class OptionsTransaction
    {
        readonly OptionsStore store;
        readonly Dictionary<Option, object> snapshot = new Dictionary<Option, object>();
        readonly Dictionary<Option, object> pending = new Dictionary<Option, object>();

        public OptionsTransaction(OptionsStore store, IEnumerable<Option> options)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            foreach (var option in options ?? Enumerable.Empty<Option>())
            {
                if (option != null)
                    snapshot[option] = option.BoxedValue;
            }
        }

        /// <summary>A transaction over every registered option carrying all of <paramref name="flags"/>.</summary>
        public static OptionsTransaction ForFlagged(OptionsStore store, OptionFlags flags = OptionFlags.Confirm) =>
            new OptionsTransaction(store, store.All.Where(option => (option.Flags & flags) == flags));

        public IReadOnlyCollection<Option> Options => snapshot.Keys;

        public bool IsTracking(Option option) => snapshot.ContainsKey(option);

        /// <summary>Staged values that differ from the options' current values.</summary>
        public bool HasChanges => pending.Any(pair => !Equals(pair.Value, pair.Key.BoxedValue));

        /// <summary>Values were applied and await <see cref="Commit"/> or <see cref="Revert"/>.</summary>
        public bool IsApplied { get; private set; }

        public void Set<T>(Option<T> option, T value) => Set((Option)option, value);

        /// <summary>Stages a value of the option's <see cref="Option.ValueType"/>. It is not written until <see cref="Apply"/>.</summary>
        public void Set(Option option, object value)
        {
            if (!IsTracking(option))
                throw new ArgumentException($"Option '{option?.Id}' is not part of this transaction.", nameof(option));
            pending[option] = value;
        }

        /// <summary>The staged value if any, else the option's current value.</summary>
        public object Get(Option option) => pending.TryGetValue(option, out var value) ? value : option.BoxedValue;

        public T Get<T>(Option<T> option) => pending.TryGetValue(option, out var value) ? (T)value : option.Value;

        public bool IsPending(Option option) => pending.TryGetValue(option, out var value) && !Equals(value, option.BoxedValue);

        /// <summary>Writes the staged values. Appliers react through the options' change events.</summary>
        public void Apply()
        {
            if (pending.Count == 0)
                return;
            foreach (var pair in pending)
                pair.Key.BoxedValue = pair.Value;
            pending.Clear();
            IsApplied = true;
        }

        /// <summary>Writes the baseline back and drops staged values.</summary>
        public void Revert()
        {
            pending.Clear();
            foreach (var pair in snapshot)
                pair.Key.BoxedValue = pair.Value;
            IsApplied = false;
        }

        /// <summary>Makes the current values the baseline and saves the store.</summary>
        public void Commit()
        {
            Apply();
            foreach (var option in snapshot.Keys.ToList())
                snapshot[option] = option.BoxedValue;
            IsApplied = false;
            store.Save();
        }

        /// <summary>Drops staged values that were not applied yet.</summary>
        public void Discard() => pending.Clear();
    }
}
