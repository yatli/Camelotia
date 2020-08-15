using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MegaCom.UI.Views
{
    public class PortStatusView : UserControl
    {
        public PortStatusView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
