namespace toolbox.Options
{
    /// <summary>Keeps the text in memory. For tests and for sessions that must not persist.</summary>
    public sealed class InMemoryOptionsStorage : IOptionsStorage
    {
        /// <summary>The stored text, or null when nothing has been written.</summary>
        public string Text { get; set; }

        public int Writes { get; private set; }

        public bool TryRead(out string text)
        {
            text = Text;
            return text != null;
        }

        public void Write(string text)
        {
            Text = text;
            Writes++;
        }

        public void Delete() => Text = null;
    }
}
