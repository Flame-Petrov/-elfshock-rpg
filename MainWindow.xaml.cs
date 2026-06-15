using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using RPG_Game_Elfshock.ViewModels;

namespace RPG_Game_Elfshock
{
    /// <summary>
    /// Hosts the active screen and forwards keyboard input to whichever
    /// screen view model is currently shown.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.DamageFlash += PlayDamageFlash;
        }

        private void Window_OnKeyDown(object sender, KeyEventArgs e)
        {
            (_viewModel.Current as IKeyInput)?.OnKey(e.Key);
        }

        private void PlayDamageFlash()
        {
            ((Storyboard)FindResource("DamageFlash")).Begin(this);
        }
    }
}
