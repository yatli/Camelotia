using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    class DisplayMirrorViewModel : ViewModelBase
    {
        [Reactive]
        public byte[] FrameBuffer { get; set; }

        public override string Name => "Display Mirror";
    }
}
