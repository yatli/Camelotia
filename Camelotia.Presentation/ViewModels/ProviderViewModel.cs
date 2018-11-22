using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using Camelotia.Presentation.Interfaces;
using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using ReactiveUI.Fody.Helpers;
using ReactiveUI;

namespace Camelotia.Presentation.ViewModels
{
    public sealed class ProviderViewModel : ReactiveObject, IProviderViewModel
    {
        private readonly ObservableAsPropertyHelper<IEnumerable<FileModel>> _files;
        private readonly ReactiveCommand<Unit, IEnumerable<FileModel>> _refresh;
        private readonly ObservableAsPropertyHelper<bool> _isCurrentPathEmpty;
        private readonly ReactiveCommand<Unit, Unit> _downloadSelectedFile;
        private readonly ReactiveCommand<Unit, Unit> _uploadToCurrentPath;
        private readonly ObservableAsPropertyHelper<string> _currentPath;
        private readonly ObservableAsPropertyHelper<bool> _hasErrors;
        private readonly ObservableAsPropertyHelper<bool> _isLoading;
        private readonly ObservableAsPropertyHelper<bool> _isReady;
        private readonly ReactiveCommand<Unit, string> _back;
        private readonly ReactiveCommand<Unit, string> _open;
        private readonly IProvider _provider;

        public ProviderViewModel(
            IAuthViewModel authViewModel,
            IFileManager fileManager,
            IProvider provider)
        {
            _provider = provider;
            _refresh = ReactiveCommand.CreateFromTask(() => provider.Get(CurrentPath));
            _files = _refresh
                .Select(files => files
                .OrderByDescending(file => file.IsFolder)
                .ThenBy(file => file.Name)
                .ToList())
                .ToProperty(this, x => x.Files);
            
            _isLoading = _refresh
                .IsExecuting
                .ToProperty(this, x => x.IsLoading);
            
            _isReady = _refresh
                .IsExecuting
                .Select(executing => !executing)
                .ToProperty(this, x => x.IsReady);
            
            var canOpenCurrentPath = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(file => file != null && file.IsFolder)
                .CombineLatest(_refresh.IsExecuting, (folder, busy) => folder && !busy);
            
            _open = ReactiveCommand.Create(
                () => Path.Combine(CurrentPath, SelectedFile.Name), 
                canOpenCurrentPath);

            var canCurrentPathGoBack = this
                .WhenAnyValue(x => x.CurrentPath)
                .Select(path => path?.Length > 1)
                .CombineLatest(_refresh.IsExecuting, (valid, busy) => valid && !busy);
            
            _back = ReactiveCommand.Create(
                () => Path.GetDirectoryName(CurrentPath), 
                canCurrentPathGoBack);

            _currentPath = _open
                .Merge(_back)
                .DistinctUntilChanged()
                .ToProperty(this, x => x.CurrentPath, "/");

            this.WhenAnyValue(x => x.CurrentPath)
                .Select(path => Unit.Default)
                .InvokeCommand(_refresh);

            _isCurrentPathEmpty = this
                .WhenAnyValue(x => x.Files)
                .Where(files => files != null)
                .Select(files => !files.Any())
                .ToProperty(this, x => x.IsCurrentPathEmpty);

            _hasErrors = _refresh
                .ThrownExceptions
                .Select(exception => true)
                .Merge(_refresh.Select(x => false))
                .ToProperty(this, x => x.HasErrors);

            var canUploadToCurrentPath = this
                .WhenAnyValue(x => x.CurrentPath)
                .Select(path => path != null)
                .DistinctUntilChanged();
                
            _uploadToCurrentPath = ReactiveCommand.CreateFromObservable(
                () => Observable
                    .FromAsync(fileManager.OpenRead)
                    .Select(stream => _provider.UploadFile(CurrentPath, stream))
                    .SelectMany(task => task.ToObservable()), 
                canUploadToCurrentPath);

            var canDownloadSelectedFile = this
                .WhenAnyValue(x => x.SelectedFile)
                .Select(file => file != null && !file.IsFolder)
                .DistinctUntilChanged();
                
            _downloadSelectedFile = ReactiveCommand.CreateFromObservable(
                () => Observable
                    .FromAsync(fileManager.OpenWrite)
                    .Select(stream => _provider.DownloadFile(SelectedFile.Path, stream))
                    .SelectMany(task => task.ToObservable()), 
                canDownloadSelectedFile);
            
            _uploadToCurrentPath.ThrownExceptions
                .Merge(_downloadSelectedFile.ThrownExceptions)
                .Subscribe(Console.WriteLine);

            this.WhenAnyValue(x => x.SelectedFile)
                .Where(file => file != null && file.IsFolder)
                .Buffer(2, 1)
                .Select(files => (files.First().Path, files.Last().Path))
                .DistinctUntilChanged()
                .Where(x => x.Item1 == x.Item2)
                .Select(ignore => Unit.Default)
                .InvokeCommand(_open);
            
            Auth = authViewModel;
            Activator = new ViewModelActivator();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.Auth.IsAuthenticated)
                    .Where(authenticated => authenticated)
                    .Select(ignore => Unit.Default)
                    .InvokeCommand(_refresh)
                    .DisposeWith(disposable);
            });
        }
        
        public IAuthViewModel Auth { get; }
        
        public ViewModelActivator Activator { get; }

        [Reactive] public FileModel SelectedFile { get; set; }
        
        public ICommand DownloadSelectedFile => _downloadSelectedFile;

        public ICommand UploadToCurrentPath => _uploadToCurrentPath;
        
        public bool IsCurrentPathEmpty => _isCurrentPathEmpty.Value;
        
        public IEnumerable<FileModel> Files => _files.Value;

        public string Description => _provider.Description;
        
        public string CurrentPath => _currentPath?.Value;
        
        public bool IsLoading => _isLoading.Value;

        public bool HasErrors => _hasErrors.Value;

        public bool IsReady => _isReady.Value;

        public string Name => _provider.Name;
        
        public string Size => _provider.Size;

        public ICommand Refresh => _refresh;

        public ICommand Back => _back;

        public ICommand Open => _open;
    }
}