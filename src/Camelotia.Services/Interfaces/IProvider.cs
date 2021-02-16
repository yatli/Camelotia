using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Camelotia.Services.Models;

namespace Camelotia.Services.Interfaces
{
    public interface IProvider : INotifyPropertyChanged
    {
        string Name { get; }
        
        string InitialPath { get; }

        Task<IEnumerable<FileModel>> Get(string path);
        
        Task UploadFile(string to, Stream from, string name);

        Task DownloadFile(string from, long rawSize, Stream to);

        bool CanCreateFolder { get; }

        Task CreateFolder(string path, string name);

        Task RenameFile(string path, string name);

        Task Delete(string path, bool isFolder);

        long Speed { get; }

        int Progress { get; }
    }
}