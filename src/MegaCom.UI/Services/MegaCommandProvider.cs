using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Camelotia.Services.Interfaces;
using Camelotia.Services.Models;

using MegaCom;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Camelotia.Services.Providers
{
    public sealed class MegaCommandProvider : ReactiveObject, IProvider
    {
        private MegaCom.ComHost _host;

        public MegaCommandProvider(ComHost host)
        {
            _host = host;
        }

        public string InitialPath => "\\";

        public string Name => $"MegaCommand";

        public bool CanCreateFolder => true;

        [Reactive]
        public long Speed { get; set; }
        [Reactive]
        public int Progress { get; set; }

        /// <returns>true if error occurs</returns>
        private async Task<bool> cwd(string path)
        {
            Log.WriteLine($"cwd {path}");
            path = path.Replace('\\', '/');
            var cwd_payload = new List<byte>();
            cwd_payload.Add(0x00 /*FC_CWD*/);
            cwd_payload.AddRange(Encoding.UTF8.GetBytes(path));
            cwd_payload.Add(/*null string termination*/0x00);
            var tx = await _host.sendFrame(new Frame(ComType.FILESERVER, cwd_payload));
            if (tx != ComStatus.ACK) { throw new Exception("ls: cwd tx"); }
            var rx = await _host.recvFrame(ComType.FILESERVER);
            return checkFSError(rx);
        }

        public async Task<IEnumerable<FileModel>> Get(string path)
        {
            path = path.Replace('\\', '/');
            Log.WriteLine($"Get: path={path}");
            if (await cwd(path))
            {
                return Enumerable.Empty<FileModel>();
            }

            var tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER,
                new byte[] { 0x01 /* FC_LS */ }));
            if (tx != ComStatus.ACK) { throw new Exception("ls: ls tx"); }
            var files = new List<FileModel>();
            var ls = await _host.recvFrame(ComType.FILESERVER);
            while (ls.type == ComType.FILESERVER && ls.data[0] == 0x09 /*FS_RSP_DATA*/)
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

        public async Task UploadFile(string path, Stream from, string name)
        {
            path = path.Replace('\\', '/');
            Log.WriteLine($"UploadFile: path={path}, name={name}");
            await cwd(path);

            var payload = new List<byte>();
            payload.Add(0x03 /* FC_PUT_BEGIN */);
            payload.AddRange(Encoding.UTF8.GetBytes(name));
            payload.Add(/*null string termination*/0x00);

            var tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER,
                payload));
            if (tx != ComStatus.ACK) { throw new Exception("UploadFile: begin tx"); }
            var rx = await _host.recvFrame(ComType.FILESERVER);
            if (checkFSError(rx)) { throw new Exception("UploadFile: begin rx"); }

            Speed = 0;
            Stopwatch sw = Stopwatch.StartNew();
            Progress = 0;
            long sent = 0;
            long sentP = 0;
            long size = from.Length;

            var buf = new byte[512];
            while (sent != size)
            {
                int w = await from.ReadAsync(buf, 0, 512);
                if (w <= 0) break;

                payload.Clear();
                payload.Add(0x04 /* FS_PUT_DATA */);
                payload.AddRange(buf.Take(w));

                tx = await _host.sendFrame(new Frame(
                    ComType.FILESERVER,
                    payload));
                if (tx != ComStatus.ACK) { throw new Exception("UploadFile: data tx"); }
                rx = await _host.recvFrame(ComType.FILESERVER);
                if (checkFSError(rx)) { throw new Exception("UploadFile: data rx"); }

                sent += w;
                sentP += w;

                Progress = (int)(sent * 100 / size);
                if (sw.ElapsedMilliseconds > 2000)
                {
                    Speed = sentP / sw.ElapsedMilliseconds;
                    sentP = 0;
                    sw.Restart();
                }
            }

            // send_complete
            tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER,
                new byte[] { 0x05 /* FC_PUT_END */ }));
            if (tx != ComStatus.ACK) { throw new Exception("UploadFile: end tx"); }
            rx = await _host.recvFrame(ComType.FILESERVER);
            if (checkFSError(rx)) { throw new Exception("UploadFile: end rx"); }
        }

        public async Task DownloadFile(string path, long expectedSize, Stream to)
        {
            path = path.Replace('\\', '/');
            Log.WriteLine($"DownloadFile: path={path}");
            var dir = Path.GetDirectoryName(path);
            var fname = Path.GetFileName(path);
            await cwd(dir);

            var payload = new List<byte>();
            payload.Add(0x02 /*FC_GET*/);
            payload.AddRange(Encoding.UTF8.GetBytes(fname));
            payload.Add(/*null string termination*/0x00);

            Speed = 0;
            Stopwatch sw = Stopwatch.StartNew();
            long sz = 0;
            long total = 0;

            var tx = await _host.sendFrame(new Frame(
                ComType.FILESERVER, payload));
            if (tx != ComStatus.ACK) { throw new Exception("get: get tx"); }
            var ls = await _host.recvFrame(ComType.FILESERVER);
            while (ls.data[0] == 0x09 /* FS_RSP_DATA */)
            {
                var data = ls.data.Skip(1).ToArray();
                await to.WriteAsync(data, 0, data.Length);
                total += data.Length;
                sz += data.Length;
                Progress = (int)(total * 100 / expectedSize);
                if (sw.ElapsedMilliseconds > 2000)
                {
                    Speed = sz / sw.ElapsedMilliseconds;
                    sz = 0;
                    sw.Restart();
                }

                // Log.WriteLine($"get: got {data.Length} bytes");
                CancellationTokenSource cancel = new CancellationTokenSource();
                cancel.CancelAfter(1000);
                try
                {
                    ls = await _host.recvFrame(ComType.FILESERVER, cancel.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.WriteLine($"get: timed out");
                    throw new FileServerTimeoutException();
                }
            }
            to.Close();
            Log.WriteLine($"get: {path} download complete.");
        }

        /// <returns>true if error occurs</returns>
        private bool checkFSError(Frame frame)
        {
            int code = frame.data[0];
            switch (code)
            {
                case 0x08:
                    return false;
                case 0x0A:
                    string errmsg = Encoding.UTF8.GetString(frame.data.Skip(1).ToArray());
                    Log.WriteLine($"ERR: {errmsg}");
                    return true;
                default:
                    Log.WriteLine($"ERR: unknown FS return code {code}");
                    return true;
            }
        }
    }
}