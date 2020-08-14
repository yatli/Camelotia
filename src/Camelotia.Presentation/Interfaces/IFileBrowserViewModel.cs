using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Camelotia.Presentation.Interfaces
{
    public interface IFileBrowserViewModel : INotifyPropertyChanged
    {
        IProviderViewModel SelectedProvider { get; }
    }
}