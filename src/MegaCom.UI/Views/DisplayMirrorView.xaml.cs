using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;

using MegaCom.UI.ViewModels;

using ReactiveUI;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Disposables;
using System;

namespace MegaCom.UI.Views
{
    public class DisplayMirrorView : ReactiveUserControl<DisplayMirrorViewModel>
    {
        public FrameBufferView Display => this.FindControl<FrameBufferView>("Display");
        public Button Snap => this.FindControl<Button>("SnapshotButton");
        public DisplayMirrorView()
        {
            this.InitializeComponent();
            Snap.Click += (a, b) => Display.TakeSnapshot();
        }

        private void InitializeComponent()
        {
            try
            {

            AvaloniaXamlLoader.Load(this);
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
