using System.Windows.Input;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// Game over screen: shows the final score; any key starts a new game
    /// (back to character select).
    /// </summary>
    public sealed class GameOverViewModel : ViewModelBase, IKeyInput
    {
        private readonly MainViewModel _main;

        public int MonstersKilled { get; }
        public string ScoreText => $"Monsters slain: {MonstersKilled}";

        public GameOverViewModel(MainViewModel main, int monstersKilled)
        {
            _main = main;
            MonstersKilled = monstersKilled;
        }

        public void OnKey(Key key) => _main.ShowCharacterSelect();
    }
}
