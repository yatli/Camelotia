using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MegaCom.UI.Views
{
    public class DisplayMirrorView : UserControl
    {
        public DisplayMirrorView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
