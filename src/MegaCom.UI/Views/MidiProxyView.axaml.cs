using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MegaCom.UI.Views
{
    public class MidiProxyView : UserControl
    {
        public MidiProxyView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
