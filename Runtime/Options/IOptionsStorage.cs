namespace toolbox.Options
{
    /// <summary>Where the options text lives. The store does the JSON; a storage only moves text.</summary>
    public interface IOptionsStorage
    {
        /// <summary>False when nothing has been saved yet.</summary>
        bool TryRead(out string text);

        void Write(string text);

        void Delete();
    }
}
