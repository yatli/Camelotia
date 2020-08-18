using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using AvaloniaEdit;

using MegaCom.UI.ViewModels;

using ReactiveUI;

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
                    TextEditor.ScrollToEnd();
                    TextEditor.InvalidateVisual();
                };
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
