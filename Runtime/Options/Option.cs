using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace toolbox.Options
{
    /// <summary>
    /// One user-facing setting: a stable id, a value that round-trips through text, and the metadata a menu needs to
    /// show it. Declare options once per game as static fields (see <see cref="OptionsCatalogAttribute"/>), register
    /// them in an <see cref="OptionsStore"/>, and react to them with <see cref="Option{T}.Bind"/>.
    /// </summary>
    public abstract class Option
    {
        string label;
        string category;

        protected Option(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An option needs a non-empty id.", nameof(id));
            Id = id;
        }

        /// <summary>Stable, dotted key ("audio.master"). It is the persistence key: never reuse an id with a new meaning.</summary>
        public string Id { get; }

        /// <summary>Shown in menus. Defaults to the last id segment, humanized ("screenShake" becomes "Screen Shake").</summary>
        public string Label
        {
            get => label ?? (label = Humanize(LastSegment(Id)));
            set => label = value;
        }

        /// <summary>Longer help shown when the option is selected.</summary>
        public string Description { get; set; }

        /// <summary>Grouping for the automatic layout. Defaults to the first id segment, humanized ("audio" becomes "Audio").</summary>
        public string Category
        {
            get => category ?? (category = Id.IndexOf('.') < 0 ? "General" : Humanize(FirstSegment(Id)));
            set => category = value;
        }

        /// <summary>Sort key within a category; ties keep registration order.</summary>
        public int Order { get; set; }

        public OptionFlags Flags { get; set; }

        /// <summary>Which widget to use; <see cref="OptionPresentation.Default"/> lets the theme decide by type.</summary>
        public OptionPresentation Presentation { get; set; }

        /// <summary>Theme key looked up when <see cref="Presentation"/> is <see cref="OptionPresentation.Custom"/>.</summary>
        public string CustomPresentation { get; set; }

        /// <summary>Shown only while this returns true (re-evaluated whenever any option changes).</summary>
        public Func<bool> VisibleWhen { get; set; }

        /// <summary>Interactable only while this returns true (re-evaluated whenever any option changes).</summary>
        public Func<bool> EnabledWhen { get; set; }

        /// <summary>Exists on this platform or build at all. Unavailable options are never shown but keep their value.</summary>
        public Func<bool> AvailableWhen { get; set; }

        /// <summary>Earlier ids this option was saved under. Read when the current id is missing, dropped on the next save.</summary>
        public string[] LegacyIds { get; set; }

        /// <summary>The store this option is registered in, or null.</summary>
        public OptionsStore Store { get; internal set; }

        public bool HasFlag(OptionFlags flag) => (Flags & flag) != 0;
        public bool IsAvailable => AvailableWhen == null || AvailableWhen();
        public bool IsVisible => IsAvailable && !HasFlag(OptionFlags.Hidden) && (VisibleWhen == null || VisibleWhen());
        public bool IsEnabled => EnabledWhen == null || EnabledWhen();

        public abstract Type ValueType { get; }

        /// <summary>The value as an object. Setting accepts the value type, its text form, or a convertible primitive.</summary>
        public abstract object BoxedValue { get; set; }
        public abstract object BoxedDefault { get; }
        public abstract bool IsDefault { get; }
        public abstract void Reset();

        /// <summary>The persisted text form (invariant culture).</summary>
        public abstract string Serialize();

        /// <summary>Parses the persisted text form and sets the value. False when the text is not valid for this option.</summary>
        public abstract bool TrySetFromString(string text);

        /// <summary>Parses the text form without setting anything. For text fields that must validate before writing.</summary>
        public abstract bool TryParseValue(string text, out object value);

        /// <summary>The value as a menu shows it.</summary>
        public abstract string DisplayValue { get; }

        /// <summary>Raised after the value changed. For the typed value, see <see cref="Option{T}.ValueChanged"/>.</summary>
        public event Action Changed;

        protected void RaiseChanged()
        {
            Changed?.Invoke();
            Store?.NotifyChanged(this);
        }

        public override string ToString() => $"{Id} = {Serialize()}";

        static string FirstSegment(string id)
        {
            int dot = id.IndexOf('.');
            return dot < 0 ? id : id.Substring(0, dot);
        }

        static string LastSegment(string id)
        {
            int dot = id.LastIndexOf('.');
            return dot < 0 ? id : id.Substring(dot + 1);
        }

        /// <summary>"muteInBackground" becomes "Mute In Background", "fps_cap" becomes "Fps Cap", "VSync" stays "VSync".</summary>
        public static string Humanize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new StringBuilder(text.Length + 4);
            bool startOfWord = true;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '_' || c == '-' || c == ' ')
                {
                    if (result.Length > 0 && result[result.Length - 1] != ' ')
                        result.Append(' ');
                    startOfWord = true;
                    continue;
                }

                bool breaksWord = i > 0 && char.IsUpper(c) && (char.IsLower(text[i - 1]) || char.IsDigit(text[i - 1]));
                if (breaksWord)
                {
                    result.Append(' ');
                    startOfWord = true;
                }

                result.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                startOfWord = false;
            }

            return result.ToString();
        }
    }

    /// <summary>A typed option. Subclasses define the text form, coercion (clamping, snapping, choice validation) and display.</summary>
    public abstract class Option<T> : Option
    {
        T value;

        protected Option(string id, T defaultValue) : base(id)
        {
            Default = defaultValue;
            value = defaultValue;
        }

        public T Default { get; }

        /// <summary>The committed value. Setting coerces it and raises change events when it differs.</summary>
        public T Value
        {
            get => value;
            set => Set(value);
        }

        /// <summary>Raised with the new value after it changed.</summary>
        public event Action<T> ValueChanged;

        /// <summary>Overrides <see cref="DisplayValue"/>.</summary>
        public Func<T, string> Formatter { get; set; }

        /// <summary>Coerces and sets the value. True when it changed.</summary>
        public bool Set(T newValue)
        {
            newValue = Coerce(newValue);
            if (AreEqual(value, newValue))
                return false;
            value = newValue;
            ValueChanged?.Invoke(newValue);
            RaiseChanged();
            return true;
        }

        /// <summary>Clamp, snap or validate a candidate value. The default accepts it as is.</summary>
        protected virtual T Coerce(T candidate) => candidate;

        protected virtual bool AreEqual(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        public override Type ValueType => typeof(T);

        public override object BoxedValue
        {
            get => value;
            set => Value = Unbox(value);
        }

        public override object BoxedDefault => Default;
        public override bool IsDefault => AreEqual(value, Default);
        public override void Reset() => Value = Default;
        public override string Serialize() => ToText(value);
        public override string DisplayValue => Formatter != null ? Formatter(value) : FormatValue(value);

        public override bool TrySetFromString(string text)
        {
            if (!TryParseText(text, out var parsed))
                return false;
            Value = parsed;
            return true;
        }

        public override bool TryParseValue(string text, out object value)
        {
            bool parsed = TryParseText(text, out var typed);
            value = parsed ? typed : null;
            return parsed;
        }

        protected abstract string ToText(T value);
        protected abstract bool TryParseText(string text, out T value);
        protected virtual string FormatValue(T value) => ToText(value);

        protected virtual T Unbox(object boxed)
        {
            switch (boxed)
            {
                case T typed:
                    return typed;
                case null:
                    return default;
                case string text when TryParseText(text, out var parsed):
                    return parsed;
            }

            if (typeof(T).IsEnum)
                return (T)Enum.ToObject(typeof(T), boxed);
            return (T)Convert.ChangeType(boxed, typeof(T), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The applier pattern: calls <paramref name="apply"/> with the current value now, or once the option's store has
        /// loaded, and again after every change. Dispose the result to stop. Register the option before binding so the
        /// initial call carries the loaded value instead of the default.
        /// </summary>
        public IDisposable Bind(Action<T> apply)
        {
            if (apply == null)
                throw new ArgumentNullException(nameof(apply));
            return new Binding(this, apply);
        }

        sealed class Binding : IDisposable
        {
            readonly Option<T> option;
            readonly Action<T> apply;
            readonly OptionsStore store;
            bool applied;

            public Binding(Option<T> option, Action<T> apply)
            {
                this.option = option;
                this.apply = apply;
                option.ValueChanged += OnChanged;
                store = option.Store;
                if (store != null && !store.IsLoaded)
                    store.Loaded += OnLoaded;
                else
                    ApplyCurrent();
            }

            void ApplyCurrent()
            {
                applied = true;
                apply(option.value);
            }

            void OnChanged(T newValue)
            {
                applied = true;
                apply(newValue);
            }

            void OnLoaded()
            {
                store.Loaded -= OnLoaded;
                // Load already notified the binding when the stored value differed from the default.
                if (!applied)
                    ApplyCurrent();
            }

            public void Dispose()
            {
                option.ValueChanged -= OnChanged;
                if (store != null)
                    store.Loaded -= OnLoaded;
            }
        }
    }
}
