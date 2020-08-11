using MegaCom;

using System.Linq;
using System.Text;

namespace Camelotia.Services.Providers
{
    internal class LsItem
    {
        public LsItem(Frame ls)
        {
            var data = ls.data;
            IsDirectory = data[1] != 0;
            Name = Encoding.UTF8.GetString(data.Skip(2).ToArray());
        }

        public bool IsDirectory { get; }
        public string Name { get; }
    }
}