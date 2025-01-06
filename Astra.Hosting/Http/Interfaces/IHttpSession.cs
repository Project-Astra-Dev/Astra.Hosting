using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpSession
    {
        string SessionId { get; }
        string SessionType { get; }
        DateTime ExpiresAt { get; }
        List<string> Roles { get; }
        List<string> Scopes { get; }
        Dictionary<string, string> Claims { get; }

        bool IsExpired();
        void AddScope(string scope);
        void RemoveScope(string scope);

        void AddClaim(string key, string value);
        T GetClaim<T>(string key);
        void RemoveClaim(string key);

        void Extend(TimeSpan duration);
    }
}
