using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Camelotia.Presentation.Interfaces;
using Camelotia.Services.Interfaces;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using Camelotia.Presentation.ViewModels;

namespace MegaCom.UI.ViewModels
{
    public sealed class FileBrowserViewModel : ViewModelBase, IFileBrowserViewModel
    {
        public FileBrowserViewModel(IFileManager fm, IProvider provider, Func<IFileViewModel,string> validator)
        {
            SelectedProvider = new ProviderViewModel(
                vm => new CreateFolderViewModel(vm, provider),
                vm => new RenameFileViewModel(vm, provider),
                (file, vm) => new FileViewModel(vm, file),
                fm,
                provider,
                validator);
        }
        
        public IProviderViewModel SelectedProvider { get; }

        public override string Name => "File Browser";
    }
}