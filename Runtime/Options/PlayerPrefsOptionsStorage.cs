using UnityEngine;

namespace toolbox.Options
{
    /// <summary>The whole options text in one <see cref="PlayerPrefs"/> string. For platforms without a writable file system.</summary>
    public sealed class PlayerPrefsOptionsStorage : IOptionsStorage
    {
        public PlayerPrefsOptionsStorage(string key = "toolbox.options")
        {
            Key = key;
        }

        public string Key { get; }

        public bool TryRead(out string text)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                text = null;
                return false;
            }

            text = PlayerPrefs.GetString(Key);
            return true;
        }

        public void Write(string text)
        {
            PlayerPrefs.SetString(Key, text);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
