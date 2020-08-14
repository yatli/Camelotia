using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom
{
    public enum ComType : byte
    {
        EXTUI, // button input, ext screen etc.
        FILESERVER, // simple FTP-ish protocol
        EXTMIDI, // note in, CV out etc.
        DEBUG, // plain text messages or breakpoint information
        MAX,
        UNSUPPORTED = 0xFD,
        ACK = 0xFE,
        REQUEST_RESEND = 0xFF
    }
}
