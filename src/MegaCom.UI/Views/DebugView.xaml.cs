using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

using AvaloniaEdit;

using MegaCom.UI.ViewModels;

using ReactiveUI;

using System.Reflection;

namespace MegaCom.UI.Views
{
    public class DebugView : ReactiveUserControl<DebugViewModel>
    {
        private TextEditor editor;

        public DebugView()
        {
            this.InitializeComponent();
            editor = this.FindControl<TextEditor>("LogView");

            this.WhenActivated(disposables =>
            {
                ViewModel.Updated += () =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // hack until we have https://github.com/AvaloniaUI/AvaloniaEdit/pull/107
                        var sv = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
                        sv.Offset = sv.Offset.WithY(100000);
                    });
                };
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
