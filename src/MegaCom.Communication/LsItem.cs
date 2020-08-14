using System;
using System.Linq;
using System.Text;

namespace MegaCom
{
    public class LsItem
    {
        public LsItem(Frame ls)
        {
            var data = ls.data;
            IsDirectory = data[1] != 0;

            byte[] szarr = data.Skip(2).Take(4).ToArray();
            // MegaCom is big-endian
            if(BitConverter.IsLittleEndian) { Array.Reverse(szarr); }

            Size = BitConverter.ToUInt32(szarr, 0);
            Name = Encoding.UTF8.GetString(data.Skip(6).ToArray());
        }

        public uint Size { get; }
        public bool IsDirectory { get; }
        public string Name { get; }
    }
}