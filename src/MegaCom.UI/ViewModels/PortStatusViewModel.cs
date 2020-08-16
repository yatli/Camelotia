using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using DynamicData.Binding;
using System.Reactive.Linq;
using System.IO.Ports;

namespace MegaCom.UI.ViewModels
{
    class PortStatusViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private ComHost m_host;

        [Reactive]
        public bool Connected { get; set; }
        [ObservableAsProperty]
        public string StatusText { get; } 
        [Reactive]
        public string PortName { get; set; }
        public string[] AvailablePorts { get; set; }

        public override string Name => "Port Status";

        public PortStatusViewModel(ComHost _host)
        {
            m_host = _host;
            AvailablePorts = SerialPort.GetPortNames();

            this.WhenAnyValue(_ => _.Connected)
                .Select(_ => _ ? "Connected" : "Not connected")
                .ToPropertyEx(this, _ => _.StatusText);

            this.WhenPropertyChanged(_ => _.PortName).Subscribe(_ =>
            {
                MegaComSettings.Default.Port = PortName;
                MegaComSettings.Default.Save();
                try { m_host.OpenPort(_.Value); }
                catch { }
                Connected = m_host.Connected;
            });

            Console.WriteLine(MegaComSettings.Default.Port);
            PortName = MegaComSettings.Default.Port;
            Connected = m_host.Connected;
        }
    }
}
