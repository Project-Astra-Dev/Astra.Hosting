using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Database.Interfaces
{
    public interface IDatabaseCollectionMetadata
    {
        string CollectionName { get; }
        string CollectionType { get; }
        int MaxItems { get; }
    }
}
