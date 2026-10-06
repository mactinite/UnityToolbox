using System;
using System.Collections.Generic;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>
    /// The persisted shape: a schema version and flat key/value text pairs. Hand-editable JSON through
    /// <see cref="JsonUtility"/>. Migrations (<see cref="OptionsStore.AddMigration"/>) rewrite instances of this
    /// before values reach the options.
    /// </summary>
    [Serializable]
    public sealed class OptionsFile
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public string value;

            public Entry(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }

        public int version = 1;
        public List<Entry> values = new List<Entry>();

        public bool TryGet(string key, out string value)
        {
            // Last write wins, so a duplicated key resolves the way a hand edit would expect.
            for (int i = values.Count - 1; i >= 0; i--)
            {
                if (values[i].key == key)
                {
                    value = values[i].value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool Contains(string key) => TryGet(key, out _);

        public OptionsFile Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("A key is required.", nameof(key));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].key == key)
                {
                    values[i] = new Entry(key, value);
                    return this;
                }
            }

            values.Add(new Entry(key, value));
            return this;
        }

        public bool Remove(string key) => values.RemoveAll(entry => entry.key == key) > 0;

        /// <summary>Moves a value to a new key. When the new key already has a value, the old one is dropped.</summary>
        public bool Rename(string oldKey, string newKey)
        {
            if (!TryGet(oldKey, out var value))
                return false;
            if (!Contains(newKey))
                Set(newKey, value);
            Remove(oldKey);
            return true;
        }

        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>(values.Count);
            foreach (var entry in values)
            {
                if (!string.IsNullOrEmpty(entry.key))
                    result[entry.key] = entry.value;
            }

            return result;
        }

        public string ToJson(bool prettyPrint = true) => JsonUtility.ToJson(this, prettyPrint);

        /// <summary>Parses a file. Throws when the text is not an options file.</summary>
        public static OptionsFile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Empty options text.", nameof(json));
            var file = JsonUtility.FromJson<OptionsFile>(json);
            if (file == null)
                throw new ArgumentException("Not an options file.", nameof(json));
            file.values = file.values ?? new List<Entry>();
            return file;
        }
    }
}
