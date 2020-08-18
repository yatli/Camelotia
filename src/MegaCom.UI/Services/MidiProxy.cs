using Commons.Music.Midi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MegaCom.Services
{
    public class MidiProxy : IDisposable
    {
        CancellationTokenSource m_cancel;
        private ComHost m_host;
        private IMidiInput m_input1;
        private IMidiInput m_input2;
        private Channel<Frame> m_pending_input;

        public MidiProxy(ComHost host, string port1, string port2)
        {
            m_cancel = new CancellationTokenSource();
            m_host = host;
            m_pending_input = Channel.CreateUnbounded<Frame>();//new UnboundedChannelOptions { AllowSynchronousContinuations = true, SingleWriter = false, SingleReader = true });
            MidiInputProc(port1, port2);
            MidiOutputProc(port1, port2);
        }

        internal string[] GetAvailablePorts()
        {
            return MidiAccessManager
                .Default
                .Inputs
                .Select(_ => _.Name)
                .ToArray();
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

        private async void MidiInputProc(string port1, string port2)
        {
            var cancel = m_cancel.Token;
            var midi_access = MidiAccessManager.Default;

            var input1_desc = midi_access.Inputs.First(_ => _.Name == port1);
            var input2_desc = midi_access.Inputs.First(_ => _.Name == port2);

            m_input1 = await midi_access.OpenInputAsync(input1_desc.Id);
            m_input2 = await midi_access.OpenInputAsync(input2_desc.Id);

            m_input1.MessageReceived += inputMessageReceived;
            m_input2.MessageReceived += inputMessageReceived;

            Log.WriteLine($"Midi port 1 proxied to {port1}");
            Log.WriteLine($"Midi port 2 proxied to {port2}");

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    int n_frames = m_pending_input.Reader.Count;
                    //Console.WriteLine(n_frames);
                    if (n_frames > 0)
                    {
                        var frm1 = new Frame(ComType.EXTMIDI, new List<byte>());
                        var frm2 = new Frame(ComType.EXTMIDI, new List<byte>());

                        frm1.data.Add(0);
                        frm2.data.Add(1);

                        for (int i = 0; i < n_frames; ++i)
                        {
                            var frame = await m_pending_input.Reader.ReadAsync();
                            if (frame.data[0] == 0)
                            {
                                frm1.data.AddRange(frame.data.Skip(1));
                            }
                            else
                            {
                                frm2.data.AddRange(frame.data.Skip(1));
                            }
                        }
                        if (frm1.data.Count > 1)
                        {
                            await m_host.sendFrame(frm1, cancel);
                        }
                        if (frm2.data.Count > 1)
                        {
                            await m_host.sendFrame(frm2, cancel);
                        }
                    }
                    else
                    {
                        Frame frm = await m_pending_input.Reader.ReadAsync();
                        await m_host.sendFrame(frm, cancel);
                    }
                }
                catch (MegaComTimeoutException)
                {
                    Log.WriteLine("MIDI forward timeout");
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async void MidiOutputProc(string port1, string port2)
        {
            var cancel = m_cancel.Token;

            var midi_access = MidiAccessManager.Default;
            var output1_desc = midi_access.Outputs.First(_ => _.Name == port1);
            var output2_desc = midi_access.Outputs.First(_ => _.Name == port2);

            var output1 = await midi_access.OpenOutputAsync(output1_desc.Id);
            var output2 = await midi_access.OpenOutputAsync(output2_desc.Id);

            var parser1 = new MidiParser();
            var parser2 = new MidiParser();

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var frame = await m_host.recvFrame(ComType.EXTMIDI, cancel);
                    var port = frame.data[0];
                    MidiParser parser;
                    IMidiOutput output;
                    if (port == 0)
                    {
                        parser = parser1;
                        output = output1;
                    }
                    else
                    {
                        parser = parser2;
                        output = output2;
                    }
                    for (int i = 1; i < frame.data.Count; ++i)
                    {
                        if (parser.feed(frame.data[i], out var buf, out var len))
                        {
                            output.Send(buf, 0, len, -1);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
