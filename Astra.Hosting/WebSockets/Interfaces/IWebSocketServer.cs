using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.WebSockets.Interfaces
{
    public interface IWebSocketServer
    {
        void Start();
        void Stop();

        string Hostname { get; }
        ushort Port { get; }
    }
}
