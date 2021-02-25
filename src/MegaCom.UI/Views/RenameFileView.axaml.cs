using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Camelotia.Presentation.Interfaces;

using ReactiveUI;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MegaCom.UI.Views
{
    public sealed class RenameFileView : ReactiveUserControl<IRenameFileViewModel>
    {
        public RenameFileView()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }

        public void OnKeyDown(object sender, KeyEventArgs args)
        {
            if(args.Key == Key.Enter)
            {
                ViewModel.Rename.Execute(null);
            }
        }
    }
}