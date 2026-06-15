using System.Windows;
using RPG_Game_Elfshock.Data;

namespace RPG_Game_Elfshock
{
    /// <summary>
    /// Interaction logic for App.xaml. Ensures the SQLite database exists on startup.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new GameDbContext();
            db.Database.EnsureCreated();
        }
    }
}
