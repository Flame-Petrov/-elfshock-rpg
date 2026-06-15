using System.Windows.Input;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>MainMenu screen: shows the welcome text; any key starts the game.</summary>
    public sealed class MainMenuViewModel : ViewModelBase, IKeyInput
    {
        private readonly MainViewModel _main;

        public string Message => "WELCOME!\n\nPress any key to play.";

        public MainMenuViewModel(MainViewModel main) => _main = main;

        public void OnKey(Key key) => _main.ShowCharacterSelect();
    }
}
