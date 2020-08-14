using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

using Akavache;

using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;

using MegaCom;

namespace Camelotia.Services.Providers
{
    public sealed class MegaCommandProvider : IProvider
    {
        private readonly ProviderModel _model;
        private readonly ISubject<bool> _isAuthorized = new ReplaySubject<bool>();
        private MegaCom.ComHost _host;
        private MegaCom.MidiProxy _midi;
        private readonly IBlobCache _blobCache;

        public MegaCommandProvider(ProviderModel model, IBlobCache _cache)
        {
            _model = model;
            _blobCache = _cache;
            _isAuthorized.OnNext(false);
            _host = null;
            EnsureLoggedInIfTokenSaved();
        }

        public long? Size => null;

        public Guid Id => _model.Id;

        public string InitialPath => "\\";

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
            bool ok = openComlink(login);
            if (ok) {
                var persistentId = Id.ToString();
                var model = await _blobCache.GetObject<ProviderModel>(persistentId);
                model.Token = password;
                model.User = login;
                await _blobCache.InsertObject(persistentId, model);
            }
            _isAuthorized.OnNext(ok);
        }

        private bool openComlink(string login)
        {
            try
            {
                _host?.Dispose();
                _host = null;
                _host = new ComHost(login);
                _midi = new MidiProxy(_host);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task Logout()
        {
            _midi?.Dispose();
            _midi = null;
            _host?.Dispose();
            _host = null;
            _isAuthorized.OnNext(false);
            return Task.CompletedTask;
        }

        private async Task cwd(string path)
        {
            path = path.Replace('\\', '/');
            var cwd_payload = new List<byte>();
            cwd_payload.Add(/*FC_CWD*/0x00);
            cwd_payload.AddRange(Encoding.UTF8.GetBytes(path));
            cwd_payload.Add(/*null string termination*/0x00);
            var tx = await _host.sendFrame(new Frame(ComType.FILESERVER, cwd_payload));
            if (tx != ComStatus.ACK) { throw new Exception("ls: cwd tx"); }
            var rx = await _host.recvFrame(ComType.FILESERVER);
            if (rx.data[0] != 0x06 /*FS_OK*/)
            {
                if (rx.data[0] == 0x08 /*FS_ERROR*/)
                {
                    string errmsg = Encoding.UTF8.GetString(rx.data.Skip(1).ToArray());
                    Log.WriteLine($"ERR: {errmsg}");
                }
                throw new Exception("ls: cwd not ok");
            }

            Log.WriteLine("ls: cwd ok");
        }

        public async Task<IEnumerable<FileModel>> Get(string path)
        {
            path = path.Replace('\\', '/');
            Log.WriteLine($"Get: path={path}");
            await cwd(path);

            var tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER,
                new byte[] { /* FC_LS */ 0x01 }));
            if (tx != ComStatus.ACK) { throw new Exception("ls: ls tx"); }
            var files = new List<FileModel>();
            var ls = await _host.recvFrame(ComType.FILESERVER);
            while (ls.type == ComType.FILESERVER && ls.data[0] == 0x07)
            {
                var ls_item = new LsItem(ls);
                Log.WriteLine($"ls item: {ls_item.Name}");
                files.Add(new FileModel
                {
                    Name = ls_item.Name,
                    IsFolder = ls_item.IsDirectory,
                    Size = ls_item.Size,
                    Path = Path.Combine(path, ls_item.Name),
                });
                ls = await _host.recvFrame(ComType.FILESERVER);
            }
            return files;
        }

        public async Task CreateFolder(string path, string name)
        {
        }

        public async Task RenameFile(string path, string name)
        {
        }

        public async Task Delete(string path, bool isFolder)
        {
        }

        public async Task UploadFile(string to, Stream from, string name)
        {
        }

        public async Task DownloadFile(string path, Stream to)
        {
            path = path.Replace('\\', '/');
            Log.WriteLine($"DownloadFile: path={path}");
            var dir = Path.GetDirectoryName(path);
            var fname = Path.GetFileName(path);
            await cwd(dir);

            var get_payload = new List<byte>();
            get_payload.Add(/*FC_GET*/0x02);
            get_payload.AddRange(Encoding.UTF8.GetBytes(fname));
            get_payload.Add(/*null string termination*/0x00);

            var tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER, get_payload));
            if (tx != ComStatus.ACK) { throw new Exception("get: get tx"); }
            var ls = await _host.recvFrame(ComType.FILESERVER);
            while (ls.data[0] == 0x07)
            {
                var data = ls.data.Skip(1).ToArray();
                await to.WriteAsync(data, 0, data.Length);
                Log.WriteLine($"get: got {data.Length} bytes");
                ls = await _host.recvFrame(ComType.FILESERVER);
            }
            to.Close();
        }

        private async void EnsureLoggedInIfTokenSaved()
        {
            var persistentId = Id.ToString();
            var model = await _blobCache.GetOrFetchObject(persistentId, () => Task.FromResult(default(ProviderModel)));
            if (model?.User == null) return;   

            _isAuthorized.OnNext(openComlink(model?.User));
        }
    }
}