using Avalonia.Threading;

using AvaloniaEdit.Document;
using AvaloniaEdit.Text;

using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    public class DebugViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private ComHost m_host;

        [Reactive]
        public TextDocument Document { get; set; }

        public DebugViewModel(ComHost host)
        {
            this.m_host = host;
            this.Document = new TextDocument();
            Log.LogWritten += s => Append($"{DateTime.Now} [HOST] {s}\n");
            Document.UndoStack.SizeLimit = 1;
            DebugProc();
        }

        private async void DebugProc()
        {
            while (true)
            {
                var debug = await m_host.recvFrame(ComType.DEBUG);
                var msg = Encoding.UTF8.GetString(debug.data.ToArray());
                Append($"{DateTime.Now} [MC]   {msg}\n");
            }
        }

        private void Append(string v)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Document.Insert(Document.TextLength, v);
                if (Document.LineCount > 1024)
                {
                    var line = Document.Lines[0];
                    Document.Remove(line.Offset, line.Length + 1);
                }
            });
            Updated();
        }

        public override string Name => "Debug";

        public event Action Updated = delegate { };
    }
}
