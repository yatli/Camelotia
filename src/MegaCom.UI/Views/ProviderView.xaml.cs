using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using Camelotia.Presentation.Interfaces;
using ReactiveUI;

namespace MegaCom.UI.Views
{
    public sealed class ProviderView : ReactiveUserControl<IProviderViewModel>
    {
        public ProviderView()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }

        public void ActivateFileList()
        {
            var list = this.FindControl<ListBox>("FilesList");
            list.Selection.SelectedIndex = 0;
            var item = list.SelectedItem;
            list.ScrollIntoView(0);
        }

        public void OnListBoxKeyDown(object sender, KeyEventArgs args)
        {
            switch(args.Key)
            {
                case Key.Up:
                case Key.Down:
                    ActivateFileList();
                    break;
                case Key.Enter:
                    if (ViewModel.Open.CanExecute(null))
                    {
                        ViewModel.Open.Execute(null);
                    }
                    break;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs args)
        {
            switch(args.Key)
            {
                case Key.F2:
                    if (ViewModel.Rename.Open.CanExecute(null))
                    {
                        ViewModel.Rename.Open.Execute(null);
                    }
                    break;
                case Key.Up:
                case Key.Down:
                    ActivateFileList();
                    break;
                case Key.Back:
                    if (ViewModel.Back.CanExecute(null))
                    {
                        ViewModel.Back.Execute(null);
                    }
                    break;
            }
        }
    }
}