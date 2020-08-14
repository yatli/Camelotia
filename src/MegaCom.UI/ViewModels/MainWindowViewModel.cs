using Camelotia.Presentation.ViewModels;

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
        private ViewModelBase[] m_vms;
        public MainWindowViewModel(FileBrowserViewModel file_vm, DisplayMirrorViewModel disp_vm)
        {
            m_vms = new ViewModelBase[] { file_vm, disp_vm };
            CurrentViewModel = m_vms[0];
            SelectedIndex = 0;
            this.WhenPropertyChanged(_ => _.SelectedIndex).Subscribe(x =>
            {
                CurrentViewModel = m_vms[x.Value];
            });
        }

        [Reactive]
        public ViewModelBase CurrentViewModel { get; set; }

        [Reactive]
        public int SelectedIndex { get; set; }
    }
}
