using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using AvaloniaEdit;

using MegaCom.UI.ViewModels;

using ReactiveUI;

using System.Reflection;

namespace MegaCom.UI.Views
{
    public class DebugView : ReactiveUserControl<DebugViewModel>
    {
        public TextEditor TextEditor => this.FindControl<TextEditor>("LogView");
        public DebugView()
        {
            this.InitializeComponent();
            this.WhenActivated(disposables =>
            {
                ViewModel.Updated += () =>
                {
                    // hack until we have https://github.com/AvaloniaUI/AvaloniaEdit/pull/107
                    var sv = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TextEditor);
                    if (sv != null)
                    {
                        sv.Offset = sv.Offset.WithY(100000);
                    }
                };
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
