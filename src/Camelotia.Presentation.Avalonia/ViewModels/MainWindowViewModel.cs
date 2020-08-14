using Camelotia.Presentation.ViewModels;

using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom.UI.ViewModels
{
    class MainWindowViewModel
    {
        public MainWindowViewModel(FileBrowserViewModel file_vm)
        {
            FileBrowser = file_vm;
        }

        public FileBrowserViewModel FileBrowser { get; }
    }
}
