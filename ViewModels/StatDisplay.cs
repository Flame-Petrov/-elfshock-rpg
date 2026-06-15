namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// One stat row in the selection summary. It is highlighted (green) only when
    /// the player actually spent buff points on it, and then shows the amount added.
    /// </summary>
    public sealed class StatDisplay
    {
        public StatDisplay(string name, int value, int added)
        {
            Name = name;
            Value = value;
            Added = added;
        }

        public string Name { get; }
        public int Value { get; }
        public int Added { get; }

        public bool HasPoints => Added > 0;

        public string Text => HasPoints ? $"{Name} {Value} (+{Added})" : $"{Name} {Value}";
    }
}
