namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// One selectable hero class in the menu. IsSelected drives the arrow and the
    /// highlight as the player moves up/down the list.
    /// </summary>
    public sealed class ClassOption : ViewModelBase
    {
        public string Name { get; }
        public string Stats { get; }
        public string Special { get; }

        public ClassOption(string name, string stats, string special)
        {
            Name = name;
            Stats = stats;
            Special = special;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
