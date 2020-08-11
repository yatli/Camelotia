using System;
using System.Collections.Generic;
using System.Text;

namespace MegaCom
{
    enum RxState
    {
        SYNC,
        TYPE,
        LEN1,
        LEN2,
        DATA,
        CHECKSUM,
    }
}
