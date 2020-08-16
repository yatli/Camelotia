using AvaloniaEdit.Document;
using AvaloniaEdit.Text;

using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    class DebugViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private ComHost m_host;

        [Reactive]
        public TextDocument Document { get; set; }

        public DebugViewModel(ComHost host)
        {
            this.m_host = host;
            this.Document = new TextDocument();
            Log.LogWritten += s => AppendLine($"{DateTime.Now} [HOST] {s}\n");
            DebugProc();
        }

        private async void DebugProc()
        {
            while (true)
            {
                var debug = await m_host.recvFrame(ComType.DEBUG);
                var msg = Encoding.UTF8.GetString(debug.data.ToArray());
                Console.WriteLine($"{DateTime.Now} [MC]   {msg}\n");
            }
        }

        private void AppendLine(string v)
        {
            Document.BeginUpdate();
            Document.Insert(Document.TextLength, $"{v}");
            if (Document.LineCount > 1000)
            {
                Document.Remove(Document.Lines[0]);
            }
            Document.EndUpdate();
        }

        public override string Name => "Debug";
    }
}
