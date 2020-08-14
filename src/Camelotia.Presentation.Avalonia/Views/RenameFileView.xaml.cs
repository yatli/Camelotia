using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Camelotia.Presentation.Interfaces;
using ReactiveUI;

namespace MegaCom.UI.Views
{
    public sealed class RenameFileView : ReactiveUserControl<IRenameFileViewModel>
    {
        public RenameFileView()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}