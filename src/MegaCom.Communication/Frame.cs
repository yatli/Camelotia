using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MegaCom
{
    public class Frame
    {
        public Frame(ComType _type, ComType _replytype) {
            type = _type;
            data = null;
            reply_type = _replytype;
        }

        public Frame(ComType _type, List<byte> _data) {
            type = _type;
            data = _data;
        }

        public Frame(ComType _type, byte[] _data)
        {
            type = _type;
            data = _data.ToList();
        }

        public override string ToString()
        {
            return $"{type}, {String.Join(" ", data?.Select(_ => $"0x{_:X2}") ?? Enumerable.Empty<string>())}";
        }

        public ComType type;
        public List<byte> data;
        public ComType? reply_type;
    }
}
