using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>
    /// The registered options of one game (or profile) and their persistence. Several catalogues can register into
    /// one store: the game's own, the standard packs, a third-party module's. Load once at startup; save when a menu
    /// commits a change, or let <see cref="SaveOnQuit"/> catch anything unsaved.
    /// </summary>
    public sealed class OptionsStore : IDisposable
    {
        static OptionsStore defaultStore;

        /// <summary>
        /// The process-wide store for games with one options file: created lazily, or assign your own. With domain
        /// reload disabled, register options at <see cref="RuntimeInitializeLoadType.AfterAssembliesLoaded"/> or
        /// later, because the default is reset during subsystem registration.
        /// </summary>
        public static OptionsStore Default
        {
            get => defaultStore ?? (defaultStore = new OptionsStore());
            set => defaultStore = value;
        }

        public static bool HasDefault => defaultStore != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            defaultStore?.Dispose();
            defaultStore = null;
        }

        readonly Dictionary<string, Option> byId = new Dictionary<string, Option>();
        readonly List<Option> ordered = new List<Option>();
        readonly Dictionary<string, string> extraValues = new Dictionary<string, string>();
        readonly SortedDictionary<int, Action<OptionsFile>> migrations = new SortedDictionary<int, Action<OptionsFile>>();
        int loadedVersion;
        bool quitHooked;

        /// <param name="storage">Defaults to <c>options.json</c> in the persistent data path.</param>
        public OptionsStore(IOptionsStorage storage = null)
        {
            Storage = storage ?? new FileOptionsStorage();
        }

        public IOptionsStorage Storage { get; set; }

        /// <summary>The game's schema version, written to the file. Raise it and add a migration when the saved shape changes.</summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>True after the first <see cref="Load"/>. Bindings made before that apply once loading finishes.</summary>
        public bool IsLoaded { get; private set; }

        public bool IsLoading { get; private set; }

        /// <summary>True when a value changed since the last load or save.</summary>
        public bool IsDirty { get; private set; }

        /// <summary>Save unsaved changes when the application quits (after the first load).</summary>
        public bool SaveOnQuit { get; set; } = true;

        /// <summary>Raised after any registered option changed, including during <see cref="Load"/>.</summary>
        public event Action<Option> OptionChanged;

        /// <summary>Raised after <see cref="Load"/> finished.</summary>
        public event Action Loaded;

        public event Action Saved;

        /// <summary>Registered options, in registration order.</summary>
        public IReadOnlyList<Option> All => ordered;

        public int Count => ordered.Count;

        /// <summary>Category names in the order they were first registered.</summary>
        public IEnumerable<string> Categories
        {
            get
            {
                var seen = new HashSet<string>();
                foreach (var option in ordered)
                {
                    if (seen.Add(option.Category))
                        yield return option.Category;
                }
            }
        }

        /// <summary>Options of one category by <see cref="Option.Order"/>, then registration order.</summary>
        public IEnumerable<Option> InCategory(string category) =>
            ordered.Where(option => option.Category == category).OrderBy(option => option.Order);

        public OptionsStore Register(params Option[] options) => Register((IEnumerable<Option>)options);

        /// <summary>Registers options. Null entries are skipped; an id registered twice with different instances throws.</summary>
        public OptionsStore Register(IEnumerable<Option> options)
        {
            if (options == null)
                return this;
            foreach (var option in options)
            {
                if (option == null)
                    continue;
                if (byId.TryGetValue(option.Id, out var existing))
                {
                    if (ReferenceEquals(existing, option))
                        continue;
                    throw new ArgumentException($"An option with id '{option.Id}' is already registered.");
                }

                if (option.Store != null && option.Store != this)
                    throw new InvalidOperationException($"Option '{option.Id}' is registered in another store.");

                option.Store = this;
                byId.Add(option.Id, option);
                ordered.Add(option);
                if (IsLoaded)
                    AdoptExtraValue(option);
            }

            return this;
        }

        /// <summary>Registers the static option fields of a <see cref="OptionsCatalogAttribute"/> class: <c>store.RegisterCatalog(typeof(MyOptions))</c>.</summary>
        public OptionsStore RegisterCatalog(Type catalogType) => Register(OptionsCatalog.Collect(catalogType));

        public Option Get(string id) => byId.TryGetValue(id, out var option) ? option : null;

        public TOption Get<TOption>(string id) where TOption : Option => Get(id) as TOption;

        public bool TryGet(string id, out Option option) => byId.TryGetValue(id, out option);

        public bool Contains(string id) => byId.ContainsKey(id);

        /// <summary>
        /// Rewrites files saved at <paramref name="fromVersion"/> into the shape of the next version. Migrations run
        /// in order from the file's version up to <see cref="SchemaVersion"/>; the store bumps the version itself.
        /// </summary>
        public OptionsStore AddMigration(int fromVersion, Action<OptionsFile> migrate)
        {
            migrations[fromVersion] = migrate ?? throw new ArgumentNullException(nameof(migrate));
            return this;
        }

        /// <summary>
        /// Reads the storage. Registered options take their saved value (or a legacy id's), missing or unreadable
        /// ones go back to their default, unknown keys are kept for the next save. No file means defaults.
        /// </summary>
        public void Load()
        {
            IsLoading = true;
            try
            {
                extraValues.Clear();
                loadedVersion = 0;
                var file = ReadFile();
                if (file == null)
                {
                    foreach (var option in ordered)
                        option.Reset();
                }
                else
                {
                    Migrate(file);
                    loadedVersion = file.version;
                    var values = file.ToDictionary();
                    foreach (var option in ordered)
                    {
                        if (!TryAdopt(option, values))
                            option.Reset();
                    }

                    foreach (var pair in values)
                    {
                        if (!IsKnownKey(pair.Key))
                            extraValues[pair.Key] = pair.Value;
                    }
                }

                IsDirty = false;
                IsLoaded = true;
            }
            finally
            {
                IsLoading = false;
            }

            HookQuit();
            Loaded?.Invoke();
        }

        /// <summary>Writes every registered option plus any unknown keys that were loaded.</summary>
        public void Save()
        {
            if (!IsLoaded)
                Debug.LogWarning("[Options] Saving a store that was never loaded replaces the saved options with the current values.");

            var file = new OptionsFile { version = Math.Max(SchemaVersion, loadedVersion) };
            foreach (var option in ordered)
                file.Set(option.Id, option.Serialize());
            foreach (var pair in extraValues)
                file.Set(pair.Key, pair.Value);

            try
            {
                Storage.Write(file.ToJson());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Options] Could not save options: {e.Message}");
                return;
            }

            IsDirty = false;
            Saved?.Invoke();
        }

        public void SaveIfDirty()
        {
            if (IsDirty)
                Save();
        }

        /// <summary>Resets every option, or those of one category, to its default.</summary>
        public void ResetAll(string category = null)
        {
            foreach (var option in ordered)
            {
                if (category == null || option.Category == category)
                    option.Reset();
            }
        }

        /// <summary>One line per option for consoles and logs.</summary>
        public string DebugDump()
        {
            var text = new StringBuilder();
            foreach (var option in ordered)
            {
                text.Append(option.Id).Append(" = ").Append(option.Serialize());
                if (!option.IsDefault)
                    text.Append("  (default ").Append(option.BoxedDefault).Append(')');
                text.Append('\n');
            }

            return text.ToString();
        }

        internal void NotifyChanged(Option option)
        {
            if (!IsLoading)
                IsDirty = true;
            OptionChanged?.Invoke(option);
        }

        OptionsFile ReadFile()
        {
            string text;
            try
            {
                if (!Storage.TryRead(out text) || string.IsNullOrWhiteSpace(text))
                    return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Options] Could not read the saved options; using defaults. {e.Message}");
                return null;
            }

            try
            {
                return OptionsFile.FromJson(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Options] The saved options could not be parsed; using defaults. {e.Message}");
                return null;
            }
        }

        void Migrate(OptionsFile file)
        {
            if (file.version > SchemaVersion)
            {
                Debug.LogWarning($"[Options] The saved options are version {file.version}, newer than this build's {SchemaVersion}. Values are kept as they are.");
                return;
            }

            while (file.version < SchemaVersion)
            {
                if (migrations.TryGetValue(file.version, out var migrate))
                    migrate(file);
                file.version++;
            }
        }

        bool TryAdopt(Option option, Dictionary<string, string> values)
        {
            if (values.TryGetValue(option.Id, out var text))
                return SetOrWarn(option, text);
            if (option.LegacyIds != null)
            {
                foreach (var legacyId in option.LegacyIds)
                {
                    if (values.TryGetValue(legacyId, out text))
                        return SetOrWarn(option, text);
                }
            }

            return false;
        }

        void AdoptExtraValue(Option option)
        {
            bool wasLoading = IsLoading;
            IsLoading = true;
            try
            {
                if (extraValues.TryGetValue(option.Id, out var text))
                {
                    extraValues.Remove(option.Id);
                    SetOrWarn(option, text);
                    return;
                }

                if (option.LegacyIds == null)
                    return;
                foreach (var legacyId in option.LegacyIds)
                {
                    if (extraValues.TryGetValue(legacyId, out text))
                    {
                        extraValues.Remove(legacyId);
                        SetOrWarn(option, text);
                        return;
                    }
                }
            }
            finally
            {
                IsLoading = wasLoading;
            }
        }

        static bool SetOrWarn(Option option, string text)
        {
            if (option.TrySetFromString(text))
                return true;
            Debug.LogWarning($"[Options] '{option.Id}' could not read \"{text}\"; using its default.");
            return false;
        }

        bool IsKnownKey(string key)
        {
            if (byId.ContainsKey(key))
                return true;
            foreach (var option in ordered)
            {
                if (option.LegacyIds != null && Array.IndexOf(option.LegacyIds, key) >= 0)
                    return true;
            }

            return false;
        }

        void HookQuit()
        {
            if (quitHooked)
                return;
            quitHooked = true;
            Application.quitting += OnQuitting;
        }

        void OnQuitting()
        {
            if (SaveOnQuit && IsDirty)
                Save();
        }

        /// <summary>Unhooks the quit handler and detaches the options so they can be registered elsewhere.</summary>
        public void Dispose()
        {
            if (quitHooked)
            {
                Application.quitting -= OnQuitting;
                quitHooked = false;
            }

            foreach (var option in ordered)
                option.Store = null;
            ordered.Clear();
            byId.Clear();
            extraValues.Clear();
        }
    }
}
