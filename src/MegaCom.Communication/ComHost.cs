using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MegaCom
{
    public class ComHost : IDisposable
    {
        private SerialPort m_port;
        private List<byte> m_rxbuf;
        private RxState m_rxstate;
        private ComType m_rxtype;
        private byte m_rxchksum;
        private ushort m_rxlen;
        private ushort m_rxpending;
        private Channel<byte[]> m_rx;
        private const byte COMSYNC_TOKEN = 0x5a;
        private TaskCompletionSource<ComStatus> m_txstatus;
        private Channel<Frame>[] m_recvframes;
        private SemaphoreSlim m_portlock;
        private bool[] m_comtype_realtime;
        private CancellationTokenSource m_cancelsrc;

        public ComHost(string port)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            int speed = 250000;
            Log.LogToStdout = true;
            Log.WriteLine($"Connecting to serial port {port}@{speed}");
            m_rxbuf = new List<byte>();
            m_port = new SerialPort(port, speed);
            m_port.DataReceived += onDataReceived;
            m_port.Open();
            Log.WriteLine($"Connected to serial port {port}@{speed}");
            m_txstatus = null;
            m_rx = Channel.CreateUnbounded<byte[]>();// new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

            int ntypes = (int)ComType.MAX;
            m_recvframes = new Channel<Frame>[ntypes];
            for (int i = 0; i < ntypes; ++i)
            {
                m_recvframes[i] = Channel.CreateUnbounded<Frame>();// (new UnboundedChannelOptions { AllowSynchronousContinuations = true, SingleWriter = true, SingleReader = false });
            }

            m_portlock = new SemaphoreSlim(1);
            m_comtype_realtime = new bool[ntypes];
            m_comtype_realtime[(int)ComType.EXTMIDI] = true;
            m_comtype_realtime[(int)ComType.DEBUG] = true;

            m_cancelsrc = new CancellationTokenSource();
            RxProc();
        }

        private async void RxProc()
        {
            var cancel = m_cancelsrc.Token;

            var stream = m_rx.Reader.ReadAllAsync(cancel);

            try
            {
                await foreach (var b in stream)
                {
                    foreach (var _b in b)
                    {
                        try
                        {
                            await handleByte(_b);
                        }
                        catch (OperationCanceledException)
                        {

                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async Task handleByte(byte data)
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
                    Log.WriteLine($"RX frame: {m_rxtype}");
                    m_rxstate = RxState.SYNC;
                    await m_portlock.WaitAsync();
                    if (m_rxtype == ComType.REQUEST_RESEND)
                    {
                        m_rxbuf.Clear();
                        m_txstatus?.TrySetResult(ComStatus.RESEND);
                        m_txstatus = null;
                    }
                    else if (m_rxtype == ComType.ACK)
                    {
                        m_rxbuf.Clear();
                        m_txstatus?.TrySetResult(ComStatus.ACK);
                        m_txstatus = null;
                    }
                    else if (m_rxtype == ComType.UNSUPPORTED)
                    {
                        m_rxbuf.Clear();
                        m_txstatus?.TrySetResult(ComStatus.UNSUPPORTED);
                        m_txstatus = null;
                    }
                    else if (m_rxchksum == data)
                    {
                        // incoming data frame, ack it
                        await sendFrame_impl(new Frame(ComType.ACK), true, false, CancellationToken.None);
                        await m_recvframes[(int)m_rxtype].Writer.WriteAsync(new Frame(m_rxtype, m_rxbuf));
                        m_rxbuf = new List<byte>();
                    }
                    else
                    {
                        // wrong data in buffer, drain and request a resend
                        m_rxbuf.Clear();
                        // must be true here because we just unlocked rx state
                        await sendFrame_impl(new Frame(ComType.REQUEST_RESEND), true, false, CancellationToken.None);
                    }
                    m_portlock.Release();
                    break;
            }
        }

        public async Task<Frame> recvFrame(ComType type)
        {
            return await m_recvframes[(int)type].Reader.ReadAsync();
        }

        public async Task<Frame> recvFrame(ComType type, CancellationToken cancel)
        {
            return await m_recvframes[(int)type].Reader.ReadAsync(cancel);
        }

        public Task<ComStatus> sendFrame(Frame frame)
        {
            return sendFrame_impl(frame, false, true, CancellationToken.None);
        }

        public Task<ComStatus> sendFrame(Frame frame, CancellationToken cancel)
        {
            return sendFrame_impl(frame, false, true, cancel);
        }

        private async Task<ComStatus> sendFrame_impl(Frame frame, bool in_rx, bool require_ack, CancellationToken cancel)
        {
            if (!in_rx)
            {
                var _status = m_txstatus;
                if (_status != null)
                {
                    try
                    {
                        await _status.Task;
                    }
                    catch
                    {
                    }
                }
                await m_portlock.WaitAsync();
                _status = m_txstatus = new TaskCompletionSource<ComStatus>();

                int timeout = m_comtype_realtime[(int)frame.type] ? 10 : 200;

                var timeout_cancel = new CancellationTokenSource();
                timeout_cancel.CancelAfter(timeout);
                timeout_cancel.Token.Register(() => _status.TrySetException(new MegaComTimeoutException()));
            }

            Log.WriteLine($"TX frame: {frame.type}");

            int len = frame.data?.Count ?? 0;
            byte[] buf = new byte[5 + len];
            buf[0] = 0x5a;
            buf[1] = (byte)frame.type;
            buf[2] = (byte)(len >> 8);
            buf[3] = (byte)(len & 0xFF);

            for (int i = 0; i < len; ++i)
            {
                buf[4 + i] = frame.data[i];
            }

            buf[buf.Length - 1] = (byte)(buf.Sum(_ => _) - 0x5a);

            for(int i = 0; i < buf.Length; ++i)
            {
                // important: don't send batches. uart0 buffer will be overrun.
                // send them one by one.
                m_port.Write(buf, i, 1);
            }
            if (!in_rx)
            {
                // XXX exception
                m_portlock.Release();
            }

            if (require_ack)
            {
                return await m_txstatus.Task;
            }
            else
            {
                return ComStatus.ACK;
            }
        }

        private void onDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int len = m_port.BytesToRead;
            byte[] data = new byte[len];
            m_port.Read(data, 0, len);
            m_rx.Writer.WriteAsync(data).AsTask().Wait();
        }

        public void Dispose()
        {
            m_port.Dispose();
            foreach (var s in m_recvframes)
            {
                s.Writer.Complete();
            }
            m_portlock.Dispose();
        }
    }
}
