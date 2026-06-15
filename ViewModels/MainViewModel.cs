using System;
using RPG_Game_Elfshock.Models.Characters;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// Navigation host. Holds the active screen view model and the state shared
    /// between screens (the created hero and its saved database id).
    /// </summary>
    public sealed class MainViewModel : ViewModelBase
    {
        private ViewModelBase _current;
        public ViewModelBase Current
        {
            get => _current;
            private set => SetProperty(ref _current, value);
        }

        // Shared between CharacterSelect (creates/saves) and InGame (plays/logs).
        public Character? Hero { get; set; }
        public int HeroRecordId { get; set; }

        /// <summary>Raised when the hero takes damage, so the view can flash the screen.</summary>
        public event Action? DamageFlash;
        public void FlashDamage() => DamageFlash?.Invoke();

        // The app always opens on the main menu, so the field starts there.
        public MainViewModel() => _current = new MainMenuViewModel(this);

        public void ShowMainMenu() => Current = new MainMenuViewModel(this);
        public void ShowCharacterSelect() => Current = new CharacterSelectViewModel(this);
        public void ShowInGame() => Current = new InGameViewModel(this);
        public void ShowGameOver(int monstersKilled) => Current = new GameOverViewModel(this, monstersKilled);
    }
}
