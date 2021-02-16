using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Camelotia.Presentation.Interfaces
{
    public interface IProviderViewModel : INotifyPropertyChanged
    {
        IRenameFileViewModel Rename { get; }

        ICreateFolderViewModel Folder { get; }

        IFileViewModel SelectedFile { get; set; }
        
        IEnumerable<IFileViewModel> Files { get; }
        
        ICommand DownloadSelectedFile { get; }
        
        ICommand UploadToCurrentPath { get; }

        ICommand DeleteSelectedFile { get; }
        
        ICommand UnselectFile { get; }

        ICommand Refresh { get; }
        
        ICommand Back { get; }
        
        ICommand Open { get; }
        
        bool IsCurrentPathEmpty { get; }
        
        bool IsLoading { get; }
        
        bool IsReady { get; }
        
        bool HasErrorMessage { get; }
        
        bool CanInteract { get; }
        
        string CurrentPath { get; }
        
        string Description { get; }
        
        string Name { get; }
    }
}