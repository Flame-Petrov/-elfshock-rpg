using System.Windows.Input;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// Implemented by every screen view model so the main window can forward
    /// keyboard input to whichever screen is currently active.
    /// </summary>
    public interface IKeyInput
    {
        void OnKey(Key key);
    }
}
