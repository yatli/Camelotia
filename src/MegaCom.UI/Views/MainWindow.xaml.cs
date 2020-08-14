using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MegaCom.UI.Views
{
    public class MainWindow : Window
    {
        public Button SwitchThemeButton => this.FindControl<Button>("SwitchThemeButton");
        public MainWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
