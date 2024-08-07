using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.WebSockets.Interfaces
{
    public interface IWebSocketRouteProcessor
    {
        Task<bool> Validate(IWebSocketClient socketClient);
    }
}
