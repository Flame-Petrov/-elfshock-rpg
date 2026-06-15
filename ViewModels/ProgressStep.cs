namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// One segment of the character-creation progress bar. Its Done/Current state
    /// drives the colours in the view.
    /// </summary>
    public sealed class ProgressStep : ViewModelBase
    {
        public string Label { get; }

        public ProgressStep(string label) => Label = label;

        private bool _isDone;
        public bool IsDone
        {
            get => _isDone;
            set => SetProperty(ref _isDone, value);
        }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }
    }
}
