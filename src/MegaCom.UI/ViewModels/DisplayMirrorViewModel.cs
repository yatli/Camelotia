using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    public class DisplayMirrorViewModel : ViewModelBase
    {
        private ComHost m_host;

        [Reactive]
        public byte[] FrameBuffer { get; set; }

        [Reactive]
        public string SnapshotPrefix { get; set; }

        [Reactive]
        public bool InvertColor { get; set; }

        public DisplayMirrorViewModel(ComHost host)
        {
            m_host = host;
            DisplayMirrorProc();
        }

        private async void DisplayMirrorProc()
        {
            while(true)
            {
                var msg = await m_host.recvFrame(ComType.EXTUI);
                if (msg.data[0] == 0 && msg.data.Count == 513)
                {
                    FrameBuffer = msg.data.Skip(1).ToArray();
                }
            }
        }

        public override string Name => "Display Mirror";
    }
}
