using System.IO;
using UnityEngine;

namespace toolbox.Options
{
    /// <summary>A JSON file, by default <c>options.json</c> in <see cref="Application.persistentDataPath"/>. Writes go through a temp file.</summary>
    public sealed class FileOptionsStorage : IOptionsStorage
    {
        public FileOptionsStorage(string path = null)
        {
            Path = string.IsNullOrEmpty(path) ? DefaultPath : path;
        }

        public static string DefaultPath => System.IO.Path.Combine(Application.persistentDataPath, "options.json");

        public string Path { get; }

        public bool TryRead(out string text)
        {
            text = null;
            if (!File.Exists(Path))
                return false;
            text = File.ReadAllText(Path);
            return true;
        }

        public void Write(string text)
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temp = Path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(Path))
                File.Replace(temp, Path, null);
            else
                File.Move(temp, Path);
        }

        public void Delete()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
