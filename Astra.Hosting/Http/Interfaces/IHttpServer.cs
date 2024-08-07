using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpServer
    {
        void Start();
        void Stop();

        string Hostname { get; }
        ushort Port { get; }
        IReadOnlyList<IHttpEndpoint> Endpoints { get; }
    }
}
