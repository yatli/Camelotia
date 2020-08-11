using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MegaCom
{
    public class FileServer : IDisposable
    {
        private SerialPort m_port;
        private List<byte> m_rxbuf;
        private RxState m_rxstate;
        private ComType m_rxtype;
        private byte m_rxchksum;
        private ushort m_rxlen;
        private ushort m_rxpending;
        private const byte COMSYNC_TOKEN = 0x5a;
        private TaskCompletionSource<ComStatus> m_txstatus;
        private Queue<Frame> m_recvframes;
        private SemaphoreSlim m_recvframes_signal;

        public FileServer(string port, int speed)
        {
            Console.WriteLine($"Connecting to serial port {port}@{speed}");
            m_rxbuf = new List<byte>();
            m_port = new SerialPort(port, speed);
            m_port.DataReceived += onDataReceived;
            m_port.Open();
            m_txstatus = null;
            m_recvframes = new Queue<Frame>();
            m_recvframes_signal = new SemaphoreSlim(0);
        }

        private void handleByte(byte data)
        {
            switch (m_rxstate)
            {
                case RxState.SYNC:
                    if (data == COMSYNC_TOKEN)
                    {
                        m_rxstate = RxState.TYPE;
                        m_rxchksum = 0;
                    }
                    break;
                case RxState.TYPE:
                    m_rxtype = (ComType)data;
                    m_rxstate = RxState.LEN1;
                    m_rxchksum += data;
                    break;
                case RxState.LEN1:
                    m_rxlen = data;
                    m_rxstate = RxState.LEN2;
                    m_rxchksum += data;
                    break;
                case RxState.LEN2:
                    m_rxlen = (ushort)((m_rxlen << 8) + data);
                    m_rxpending = m_rxlen;
                    if (m_rxlen == 0)
                    {
                        m_rxstate = RxState.CHECKSUM;
                    }
                    else
                    {
                        m_rxstate = RxState.DATA;
                    }
                    m_rxchksum += data;
                    break;
                case RxState.DATA:
                    m_rxchksum += data;
                    m_rxbuf.Add(data);
                    if (--m_rxpending == 0)
                    {
                        m_rxstate = RxState.CHECKSUM;
                    }
                    break;
                case RxState.CHECKSUM:
                    Console.WriteLine($"RX frame: {m_rxtype}, {String.Join(" ", m_rxbuf.Select(_ => $"0x{_:X2}"))}");
                    m_rxstate = RxState.SYNC;
                    if (m_rxchksum == data)
                    {
                        if (m_rxtype == ComType.REQUEST_RESEND)
                        {
                            m_rxbuf.Clear();
                            m_txstatus?.SetResult(ComStatus.RESEND);
                            m_txstatus = null;
                        }
                        else if (m_rxtype == ComType.ACK)
                        {
                            m_rxbuf.Clear();
                            m_txstatus?.SetResult(ComStatus.ACK);
                            m_txstatus = null;
                        }
                        else if (m_rxtype == ComType.UNSUPPORTED)
                        {
                            m_rxbuf.Clear();
                            m_txstatus?.SetResult(ComStatus.UNSUPPORTED);
                            m_txstatus = null;
                        }
                        else
                        {
                            // incoming data frame, ack it
                            sendFrame(new Frame(ComType.ACK), false).Wait();
                            m_recvframes.Enqueue(new Frame(m_rxtype, m_rxbuf));
                            m_recvframes_signal.Release();
                            m_rxbuf = new List<byte>();
                        }
                    }
                    else
                    {
                        // wrong data in buffer, drain and request a resend
                        m_rxbuf.Clear();
                        // must be true here because we just unlocked rx state
                        sendFrame(new Frame(ComType.REQUEST_RESEND), false).Wait();
                    }
                    break;
            }
        }

        public async Task<Frame> recvFrame()
        {
            await m_recvframes_signal.WaitAsync();
            return m_recvframes.Dequeue();
        }

        public Task<ComStatus> sendFrame(Frame frame, bool requireResponse)
        {
            if (m_txstatus != null || m_rxstate != RxState.SYNC)
            {
                throw new Exception("sendFrame");
            }

            Console.WriteLine($"TX frame: {frame}");

            int len = frame.data?.Count ?? 0;
            byte[] buf = new byte[5 + len];
            buf[0] = 0x5a;
            buf[1] = (byte)frame.type;
            buf[2] = (byte)(len >> 8);
            buf[3] = (byte)(len & 0xFF);

            for(int i=0;i<len;++i)
            {
                buf[4 + i] = frame.data[i];
            }

            buf[buf.Length - 1] = (byte)(buf.Sum(_ => _) - 0x5a);
            m_port.Write(buf, 0, buf.Length);

            if (requireResponse)
            {
                m_txstatus = new TaskCompletionSource<ComStatus>();
                return m_txstatus.Task;
            }
            else
            {
                return Task.FromResult(ComStatus.ACK);
            }
        }

        private void onDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int len = m_port.BytesToRead;
            byte[] data = new byte[len];
            m_port.Read(data, 0, len);
            foreach (var b in data)
            {
                handleByte(b);
            }
        }

        public void Dispose()
        {
            m_port.Dispose();
            m_recvframes_signal.Dispose();
        }
    }
}
