namespace MegaCom.Services
{
    public class MidiParser
    {
        private enum State
        {
            WaitStatus,
            WaitData,
            WaitSysex
        }

        private byte[] buffer;
        private int offset;
        private State state;
        private byte prev_status;
        private int prev_len;
        private int pending_len;


        public MidiParser()
        {
            state = State.WaitStatus;
            buffer = new byte[512];
            offset = 0;
            pending_len = 0;
        }

        public bool feed(byte b, out byte[] buf, out int len)
        {
            buf = buffer;
            len = -1;
            switch (state)
            {
                case State.WaitStatus:

                parse_status:
                    state = State.WaitStatus;

                    if (b >= 0x80)
                    {
                        // STATUS BYTE received
                        prev_status = b;
                        buffer[0] = b;
                        offset = 1;
                        pending_len = prev_len = get_len(b);

                        if (pending_len == 0)
                        {
                            goto received;
                        }
                        else if (b == 0xF0)
                        {
                            // SYSEX BEGIN
                            state = State.WaitSysex;
                        }
                        else
                        {
                            state = State.WaitData;
                        }
                    }
                    else if (prev_status >= 0x80 && prev_status <= 0xC0)
                    {
                        // should be status but got data. running status.
                        buffer[0] = prev_status;
                        buffer[1] = b;
                        offset = 2;
                        pending_len = prev_len - 1;
                        if (pending_len != 0)
                        {
                            state = State.WaitData;
                        }
                        else
                        {
                            goto received;
                        }
                    }
                    else
                    {
                        // not running status? just skip this frame
                    }
                    break;
                case State.WaitData:
                    if (b >= 0x80)
                    {
                        // should be data but got a new frame
                        goto parse_status;
                    }
                    else
                    {
                        buffer[offset++] = b;
                        if (0 == --pending_len)
                        {
                            goto received;
                        }
                    }
                    break;
                case State.WaitSysex:
                    buffer[offset++] = b;
                    if (b == 0xF7)
                    {
                        // SYSEX END
                        goto received;
                    }
                    else if (b >= 0x80)
                    {
                        // a new frame interrupts the current SYSEX
                        goto parse_status;
                    }
                    else
                    {
                        // sysex data
                    }
                    break;
            }

            return false;

        received:
            len = offset;
            state = State.WaitStatus;
            return true;
        }

        private int get_len(byte b)
        {
            if (b >= 0x80 && b < 0xC0) { return 2; }
            else if (b >= 0xC0 && b < 0xE0) { return 1; }
            else if (b >= 0xE0 && b < 0xF0) { return 2; }
            else if (b == 0xF1) { return 1; }
            else if (b == 0xF2) { return 2; }
            else if (b == 0xF3) { return 1; }
            else if (b >= 0xF4 && b <= 0xF7) { return 0; }
            else if (b >= 0xF8 && b <= 0xFF) { return 0; }
            else return -1;
        }
    }
}
