using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MegaCom.MidiHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var comhost = new ComHost();
            var midihost = new MidiProxy(comhost);
            comhost.OpenPort("com4");
            while (true)
            {
                var debug = await comhost.recvFrame(ComType.DEBUG);
                var msg = Encoding.UTF8.GetString(debug.data.ToArray());
                Console.WriteLine($"[DEBUG] {msg}");
            }
        }
    }
}
