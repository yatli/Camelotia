using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Camelotia.Presentation.Interfaces;
using ReactiveUI;

namespace MegaCom.UI.Views
{
    public sealed class CreateFolderView : ReactiveUserControl<ICreateFolderViewModel>
    {
        public CreateFolderView()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}