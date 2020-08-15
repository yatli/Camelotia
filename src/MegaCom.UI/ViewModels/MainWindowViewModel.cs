using ReactiveUI.Fody.Helpers;
using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Text;
using DynamicData.Binding;
using System.ComponentModel;

namespace MegaCom.UI.ViewModels
{
    class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public MainWindowViewModel(params ViewModelBase[] vms)
        {
            ViewModels = vms;
            CurrentViewModel = ViewModels[0];
        }

        [Reactive]
        public ViewModelBase CurrentViewModel { get; set; }

        public ViewModelBase[] ViewModels { get; }

        public override string Name => "Main";
    }
}
