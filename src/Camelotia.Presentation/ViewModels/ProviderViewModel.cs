using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using Camelotia.Presentation.Interfaces;
using Camelotia.Services.Extensions;
using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;

namespace Camelotia.Presentation.ViewModels
{
    public sealed class ProviderViewModel : ReactiveObject, IProviderViewModel, IActivatableViewModel
    {
        private readonly ReactiveCommand<Unit, IEnumerable<FileModel>> _refresh;
        private readonly ReactiveCommand<Unit, Unit> _downloadSelectedFile;
        private readonly ReactiveCommand<Unit, Unit> _uploadToCurrentPath;
        private readonly ReactiveCommand<Unit, Unit> _deleteSelectedFile;
        private readonly ReactiveCommand<Unit, Unit> _unselectFile;
        private readonly ReactiveCommand<Unit, string> _back;
        private readonly ReactiveCommand<Unit, string> _open;
        private readonly IProvider _provider;

        public ProviderViewModel(
            CreateFolderViewModelFactory createFolder,
            RenameFileViewModelFactory createRename,
            FileViewModelFactory createFile,
            IFileManager fileManager,
            IProvider provider)
        {
            _provider = provider;
            Folder = createFolder(this);
            Rename = createRename(this);

            // canInteract                            -> _refresh can execute
            // refresh is executing                   -> IsLoading, the progress bar
            // refresh not executing                  -> IsReady, show file list
            // is folder & not refresh & can interact -> can open (cd)
            // not refresh & can interact             -> can cd up
            // path change                            -> execute _refresh
            // path change                            -> unselect file
            // not refresh & can interact             -> can upload
            // not refresh & can interact             -> can download

            var canInteract = this
                .WhenAnyValue(
                    x => x.Folder.IsVisible,
                    x => x.Rename.IsVisible,
                    x => x.IsBusy,
                    (folder, rename, busy) => !folder && !rename && !busy);
            canInteract.ToPropertyEx(this, x => x.CanInteract);

            var canOpenCurrentPath = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(file => file != null && file.IsFolder)
                .CombineLatest(canInteract, (open, interact) => open && interact);
            
            var canCurrentPathGoBack = this
                .WhenAnyValue(x => x.CurrentPath)
                .Where(path => path != null)
                .Select(path => path.Length > provider.InitialPath.Length)
                .CombineLatest(canInteract, (back, interact) => back && interact);
            
            var canUploadToCurrentPath = this
                .WhenAnyValue(x => x.CurrentPath)
                .Select(path => path != null)
                .CombineLatest(canInteract, (up, can) => up && can);
                
            var canDownloadSelectedFile = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(file => file != null && !file.IsFolder)
                .CombineLatest(canInteract, (down, can) => down && can);
                
            var canDeleteSelection = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(file => file != null && !file.IsFolder)
                .CombineLatest(canInteract, (delete, interact) => delete && interact);

            var canUnselectFile = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(selection => selection != null)
                .CombineLatest(canInteract, (unselect, interact) => unselect && interact);

            _refresh = ReactiveCommand.CreateFromTask(
                () => provider.Get(CurrentPath), canInteract);

            _refresh.Select(files => files
                    .Select(file => createFile(file, this))
                    .OrderByDescending(file => file.IsFolder)
                    .ThenBy(file => file.Name)
                    .ToList())
                .Where(files => Files == null || !files.SequenceEqual(Files))
                .ToPropertyEx(this, x => x.Files);

            _refresh.IsExecuting.ToPropertyEx(this, x => x.IsLoading);
            
            _refresh.IsExecuting
                .Skip(1)
                .Select(executing => !executing)
                .ToPropertyEx(this, x => x.IsReady);
            
            _open = ReactiveCommand.Create(
                () => Path.Combine(CurrentPath, SelectedFile.Name), canOpenCurrentPath);

            _back = ReactiveCommand.Create(
                () => Path.GetDirectoryName(CurrentPath), canCurrentPathGoBack);

            _open.Merge(_back)
                .Select(path => path ?? provider.InitialPath)
                .DistinctUntilChanged()
                .Log(this, $"Current path changed in {provider.Name}")
                .ToPropertyEx(this, x => x.CurrentPath, provider.InitialPath);

            this.WhenAnyValue(x => x.CurrentPath)
                .Skip(1)
                .Select(path => Unit.Default)
                .InvokeCommand(_refresh);

            this.WhenAnyValue(x => x.CurrentPath)
                .Subscribe(path => SelectedFile = null);

            this.WhenAnyValue(x => x.Files)
                .Skip(1)
                .Where(files => files != null)
                .Select(files => !files.Any())
                .ToPropertyEx(this, x => x.IsCurrentPathEmpty);

            _uploadToCurrentPath = ReactiveCommand.CreateFromObservable(
                () => Observable
                    .FromAsync(fileManager.OpenRead)
                    .Where(response => response.Name != null && response.Stream != null)
                    .Select(x => _provider.UploadFile(CurrentPath, x.Stream, x.Name))
                    .SelectMany(task => task.ToObservable()),
                canUploadToCurrentPath);

            _uploadToCurrentPath.InvokeCommand(_refresh);

            _downloadSelectedFile = ReactiveCommand.CreateFromObservable(
                () => Observable
                    .FromAsync(() => fileManager.OpenWrite(SelectedFile.Name))
                    .Where(stream => stream != null)
                    .Select(stream => _provider.DownloadFile(SelectedFile.Path, SelectedFile.RawSize, stream))
                    .SelectMany(task => task.ToObservable()),
                canDownloadSelectedFile);
            
            _deleteSelectedFile = ReactiveCommand.CreateFromTask(
                () => provider.Delete(SelectedFile.Path, SelectedFile.IsFolder),
                canDeleteSelection);

            _deleteSelectedFile.InvokeCommand(Refresh);

            _unselectFile = ReactiveCommand.Create(
                () => { SelectedFile = null; },
                canUnselectFile);

            var allExceptions =
                _refresh.ThrownExceptions
                .Merge(_uploadToCurrentPath.ThrownExceptions)
                .Merge(_downloadSelectedFile.ThrownExceptions)
                .Merge(_deleteSelectedFile.ThrownExceptions);

            allExceptions
                .Select(exception => true)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Merge(_refresh.Select(x => false))
                .ToPropertyEx(this, x => x.HasErrorMessage);

            _uploadToCurrentPath
                .ThrownExceptions
                .Merge(_deleteSelectedFile.ThrownExceptions)
                .Merge(_downloadSelectedFile.ThrownExceptions)
                .Merge(_refresh.ThrownExceptions)
                .Log(this, $"Exception occured in provider {provider.Name}")
                .Subscribe();

            _refresh.IsExecuting
                .CombineLatest(
                    _downloadSelectedFile.IsExecuting,
                    _uploadToCurrentPath.IsExecuting,
                    _deleteSelectedFile.IsExecuting,
                    (refresh, down, upload, delete) => refresh || down || upload || delete)
                .Subscribe(busy => this.IsBusy = busy);

            _provider
                .WhenAnyValue(x => x.Speed)
                .CombineLatest(
                    _uploadToCurrentPath.IsExecuting, _downloadSelectedFile.IsExecuting, 
                    (spd, up, down) => down ? $"Downloading, {spd} KB/s" :
                                       up   ? $"Uploading, {spd} KB/s"   :
                                       "")
                .ToPropertyEx(this, x => x.Speed);

            _uploadToCurrentPath.IsExecuting
                .CombineLatest(_downloadSelectedFile.IsExecuting, (a, b) => a || b)
                .ToPropertyEx(this, x => x.ShowTransferProgress);

            _provider
                .WhenAnyValue(x => x.Progress)
                .ToPropertyEx(this, x => x.TransferProgress);

            Activator = new ViewModelActivator();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.CanInteract)
                    .Skip(1)
                    .Where(interact => interact)
                    .Select(x => Unit.Default)
                    .InvokeCommand(_refresh)
                    .DisposeWith(disposable);
            });
        }

        [Reactive]
        public bool IsBusy { get; set; }

        [Reactive]
        public IFileViewModel SelectedFile { get; set; }

        [ObservableAsProperty]
        public bool IsCurrentPathEmpty { get; }
        
        [ObservableAsProperty]
        public IEnumerable<IFileViewModel> Files { get; }
        
        [ObservableAsProperty]
        public string CurrentPath { get; }

        [ObservableAsProperty]
        public bool IsLoading { get; }

        [ObservableAsProperty]
        public bool HasErrorMessage { get; }

        [ObservableAsProperty]
        public bool IsReady { get; }
        
        [ObservableAsProperty]
        public bool CanInteract { get; }

        [ObservableAsProperty]
        public string Speed { get; }

        [ObservableAsProperty]
        public int TransferProgress { get; }

        [ObservableAsProperty]
        public bool ShowTransferProgress { get; }

        public IRenameFileViewModel Rename { get; }  
        
        public ICreateFolderViewModel Folder { get; }

        public ViewModelActivator Activator { get; }
        
        public string Name => _provider.Name;

        public string Description => $"{_provider.Name} file system.";

        public ICommand DownloadSelectedFile => _downloadSelectedFile;

        public ICommand UploadToCurrentPath => _uploadToCurrentPath;

        public ICommand DeleteSelectedFile => _deleteSelectedFile;

        public ICommand UnselectFile => _unselectFile;

        public ICommand Refresh => _refresh;
        
        public ICommand Back => _back;

        public ICommand Open => _open;
    }
}