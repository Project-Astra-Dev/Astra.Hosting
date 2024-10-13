using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Database.Interfaces
{
    public interface IDatabase
    {
        ILiteCollection<T>? GetCollection<T>(
            bool createIfNotExisting = true
        ) where T : IDbObject;
        Guid GetGuid();

        SimpleDatabaseDescriptor Descriptor { get; }
    }
}
