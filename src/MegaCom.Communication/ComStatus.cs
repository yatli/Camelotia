using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom
{
    public enum ComStatus: byte
    {
        ACK,
        RESEND,
        UNSUPPORTED,
        TIMEOUT
    }
}
