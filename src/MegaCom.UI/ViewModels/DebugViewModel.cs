using AvaloniaEdit.Document;
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
            Document.Insert(0, "Howdy");
        }

        public override string Name => "Debug";
    }
}
