using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;
using FluentFTP;

namespace Camelotia.Services.Providers
{
    public sealed class MegaCommandProvider : IProvider
    {
        private readonly ProviderModel _model;
        private readonly ISubject<bool> _isAuthorized = new ReplaySubject<bool>();
        private SerialPort _port;

        public MegaCommandProvider(ProviderModel model)
        {
            _model = model;
            _isAuthorized.OnNext(false);
            _port = null;
        }

        public long? Size => null;

        public Guid Id => _model.Id;

        public string InitialPath => "/";

        public string Name => $"{_model.Type}-{_model.User}";

        public DateTime Created => _model.Created;

        public IObservable<bool> IsAuthorized => _isAuthorized;

        public bool SupportsDirectAuth => true;

        public bool SupportsHostAuth => false;

        public bool SupportsOAuth => false;

        public bool CanCreateFolder => true;

        public Task OAuth() => Task.CompletedTask;

        public Task HostAuth(string address, int port, string login, string password) => Task.CompletedTask;

        public async Task DirectAuth(string login, string password)
        {
            _port?.Dispose();
            _port = null;
            _port = new SerialPort(login, 250000);
        }

        public Task Logout()
        {
            _port?.Dispose();
            _port = null;
            _isAuthorized.OnNext(false);
            return Task.CompletedTask;
        }
        
        public async Task<IEnumerable<FileModel>> Get(string path)
        {
            throw new NotImplementedException();
        }

        public async Task CreateFolder(string path, string name)
        {
            throw new NotImplementedException();
        }

        public async Task RenameFile(string path, string name)
        {
            throw new NotImplementedException();
        }

        public async Task Delete(string path, bool isFolder)
        {
            throw new NotImplementedException();
        }

        public async Task UploadFile(string to, Stream from, string name)
        {
            throw new NotImplementedException();
        }

        public async Task DownloadFile(string from, Stream to)
        {
            throw new NotImplementedException();
        }
    }
}