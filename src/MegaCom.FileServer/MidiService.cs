using Commons.Music.Midi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MegaCom
{
    public class MidiService : IDisposable
    {
        CancellationTokenSource m_cancel;
        private ComHost m_host;
        private IMidiInput m_input1;
        private IMidiInput m_input2;
        private Channel<Frame> m_pending_input;

        public MidiService(ComHost host)
        {
            m_cancel = new CancellationTokenSource();
            m_host = host;
            m_pending_input = Channel.CreateUnbounded<Frame>();//new UnboundedChannelOptions { AllowSynchronousContinuations = true, SingleWriter = false, SingleReader = true });
            MidiInputProc();
            MidiOutputProc();
        }

        public void Dispose()
        {
            m_pending_input.Writer.Complete();
            m_cancel.Cancel();
            m_cancel.Dispose();
        }

        private void inputMessageReceived(object sender, MidiReceivedEventArgs e)
        {
            int output = Object.ReferenceEquals(sender, m_input1) ? 0 : 1;
            Frame frame = new Frame(ComType.EXTMIDI, new List<byte>());
            frame.data.Add((byte)output);
            frame.data.AddRange(e.Data.Skip(e.Start).Take(e.Length));
            m_pending_input.Writer.WriteAsync(frame).AsTask().Wait();
        }

        private async void MidiInputProc()
        {
            var cancel = m_cancel.Token;
            var midi_access = MidiAccessManager.Default;

            var input1_desc = midi_access.Inputs.First(_ => _.Name == "MegaCommandPort1");
            var input2_desc = midi_access.Inputs.First(_ => _.Name == "MegaCommandPort2");

            m_input1 = await midi_access.OpenInputAsync(input1_desc.Id);
            m_input2 = await midi_access.OpenInputAsync(input2_desc.Id);

            m_input1.MessageReceived += inputMessageReceived;
            m_input2.MessageReceived += inputMessageReceived;

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var frame = await m_pending_input.Reader.ReadAsync();
                    await m_host.sendFrame(frame, cancel);
                }
                catch (MegaComTimeoutException)
                {
                    Console.WriteLine("TIMEOUT");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async void MidiOutputProc()
        {
            var cancel = m_cancel.Token;

            var midi_access = MidiAccessManager.Default;
            var output1_desc = midi_access.Outputs.First(_ => _.Name == "MegaCommandPort1");
            var output2_desc = midi_access.Outputs.First(_ => _.Name == "MegaCommandPort2");

            var output1 = await midi_access.OpenOutputAsync(output1_desc.Id);
            var output2 = await midi_access.OpenOutputAsync(output2_desc.Id);

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var frame = await m_host.recvFrame(ComType.EXTMIDI, cancel);
                    //Log.WriteLine(String.Join(" ", frame.data.Select(_ => $"{_:x2}")));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

    }
}
