using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;

namespace MegaCom.UI.Views
{
    public class FrameBufferView : UserControl
    {
        public static AvaloniaProperty<byte[]> FrameBufferProperty = AvaloniaProperty.Register<FrameBufferView, byte[]>(nameof(FrameBuffer));
        public static AvaloniaProperty<bool> InvertColorProperty = AvaloniaProperty.Register<FrameBufferView, bool>(nameof(FrameBuffer));

        public byte[] FrameBuffer
        {
            get { return GetValue(FrameBufferProperty); }
            set { SetValue(FrameBufferProperty, value); }
        }

        internal async void TakeSnapshot()
        {
            var fileDialog = new SaveFileDialog();
            fileDialog.Filters.Add(new FileDialogFilter { Extensions = new List<string> { "png" }, Name = "PNG image file" });
            var name = await fileDialog.ShowAsync((Window)this.VisualRoot);
            if (name == null) return;
            var dpi = 96.0 * 1.75;
            var img_h = 160;
            var img_w = img_h * 4;
            using (var img = new RenderTargetBitmap(new PixelSize((int)(img_w * dpi / 96.0), (int)(img_h * dpi / 96.0)), new Vector(dpi, dpi)))
            {
                using (var ctx = img.CreateDrawingContext(null))
                {
                    RenderImpl(ctx, new Rect(0, 0, img_w, img_h));
                }
                img.Save(name);
            }
        }

        public bool InvertColor
        {
            get { return GetValue(InvertColorProperty); }
            set { SetValue(InvertColorProperty, value); }
        }

        public FrameBufferView()
        {
            this.InitializeComponent();
            this.WhenAnyValue(_ => _.FrameBuffer).Subscribe(_ =>
            {
                this.InvalidateVisual();
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext context)
        {
            RenderImpl(context.PlatformImpl, Bounds);
        }

        private void RenderImpl(IDrawingContextImpl context, Rect bounds)
        {
            if (FrameBuffer == null) return;

            IBrush black, white;

            if(InvertColor)
            {
                black = Brushes.Black;
                white = Brushes.White;
            }else
            {
                black = Brushes.White;
                white = Brushes.Black;
            }

            context.FillRectangle(black, bounds);

            var h = bounds.Height * 0.9;
            var w = bounds.Width * 0.9;

            if (w / 4 >= h)
            {
                w = h * 4;
            }
            else
            {
                h = w / 4;
            }

            var xoff = (bounds.Width - w) / 2;
            var yoff = (bounds.Height - h) / 2;

            var pixsize = w / 128;

            for (int y = 0; y < 32; ++y)
            {
                for (int x = 0; x < 128; ++x)
                {
                    int yd = 31 - y;
                    var idx = x + yd / 8 * 128;
                    var bit = yd % 8;
                    var data = (FrameBuffer[idx] >> bit) & 0x01;
                    var brush = (data == 0) ? black : white;
                    context.FillRectangle(brush, new Rect(xoff + x * pixsize, yoff + y * pixsize, pixsize, pixsize));
                }
            }
        }
    }
}
