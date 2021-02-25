using System.ComponentModel;

namespace Camelotia.Presentation.Interfaces
{
    public interface IFileViewModel : INotifyPropertyChanged
    {
        string Name { get; }

        IProviderViewModel Provider { get; }

        string Validation { get; }

        bool IsFolder { get; }

        bool IsFile { get; }

        string Path { get; }

        string Size { get; }

        long RawSize { get; }
    }
}
