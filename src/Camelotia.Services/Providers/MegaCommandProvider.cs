using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;

using MegaCom;

namespace Camelotia.Services.Providers
{
    public sealed class MegaCommandProvider : IProvider
    {
        private readonly ProviderModel _model;
        private readonly ISubject<bool> _isAuthorized = new ReplaySubject<bool>();
        private MegaCom.FileServer _server;

        public MegaCommandProvider(ProviderModel model)
        {
            _model = model;
            _isAuthorized.OnNext(false);
            _server = null;
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
            try
            {
                _server?.Dispose();
                _server = null;
                _server = new FileServer(login, 250000);
                _isAuthorized.OnNext(true);
            }
            catch
            {

            }
        }

        public Task Logout()
        {
            _server?.Dispose();
            _server = null;
            _isAuthorized.OnNext(false);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<FileModel>> Get(string path)
        {
            path = path.Replace('\\', '/');
            Console.WriteLine($"Get: path={path}");
            var cwd_payload = new List<byte>();
            cwd_payload.Add(/*FC_CWD*/0x00);
            cwd_payload.AddRange(Encoding.UTF8.GetBytes(path));
            cwd_payload.Add(/*null string termination*/0x00);
            var tx = await _server.sendFrame(new Frame(
                ComType.FILESERVER, cwd_payload), true);
            if (tx != ComStatus.ACK) { throw new Exception("ls: cwd tx"); }
            var rx = await _server.recvFrame();
            if (rx.data[0] != 0x06 /*FS_OK*/)
            {
                if (rx.data[0] == 0x08 /*FS_ERROR*/)
                {
                    string errmsg = Encoding.UTF8.GetString(rx.data.Skip(1).ToArray());
                    Console.WriteLine($"ERR: {errmsg}");
                }
                throw new Exception("ls: cwd not ok");
            }

            Console.WriteLine("ls: cwd ok");

            tx = await _server.sendFrame(new Frame(
                ComType.FILESERVER,
                new byte[] { /* FC_LS */ 0x01 }), true);
            if (tx != ComStatus.ACK) { throw new Exception("ls: ls tx"); }
            var files = new List<FileModel>();
            var ls = await _server.recvFrame();
            while (ls.type == ComType.FILESERVER && ls.data[0] == 0x07)
            {
                var ls_item = new LsItem(ls);
                Console.WriteLine($"ls item: {ls_item.Name}");
                files.Add(new FileModel
                {
                    Name = ls_item.Name,
                    IsFolder = ls_item.IsDirectory,
                    Path = Path.Combine(path, ls_item.Name),
                });
                ls = await _server.recvFrame();
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

        public async Task DownloadFile(string from, Stream to)
        {
        }
    }
}