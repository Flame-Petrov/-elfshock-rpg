using System.Windows.Input;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>Turns WPF keys into the letters/digits the game logic works with.</summary>
    public static class KeyHelper
    {
        public static char? ToLetter(Key key) =>
            key is >= Key.A and <= Key.Z ? (char)('A' + (key - Key.A)) : null;

        public static int? ToDigit(Key key)
        {
            if (key is >= Key.D0 and <= Key.D9)
                return key - Key.D0;
            if (key is >= Key.NumPad0 and <= Key.NumPad9)
                return key - Key.NumPad0;
            return null;
        }
    }
}
