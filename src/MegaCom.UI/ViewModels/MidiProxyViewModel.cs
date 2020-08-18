using MegaCom.Services;

using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    class MidiProxyViewModel : ViewModelBase
    {
        private ComHost m_host;
        private MidiProxy m_proxy;

        [Reactive]
        public string Port1 { get; set; }
        [Reactive]
        public string Port2 { get; set; }
        public string[] AvailablePorts { get; }

        public MidiProxyViewModel(ComHost host)
        {
            m_host = host;
            Port1 = MegaComSettings.Default.MidiPort1;
            Port2 = MegaComSettings.Default.MidiPort2;
            m_proxy = new MidiProxy(host, Port1, Port2);

            AvailablePorts = m_proxy.GetAvailablePorts();

            // TODO reopen logic
        }
        public override string Name => "Midi Proxy";
    }
}
