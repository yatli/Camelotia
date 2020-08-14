using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Camelotia.Presentation.Interfaces;
using ReactiveUI;

namespace MegaCom.UI.Views
{
    public sealed class FileBrowserView : UserControl
    {
        public FileBrowserView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}