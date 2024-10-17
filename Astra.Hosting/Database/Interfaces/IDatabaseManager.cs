using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Astra.Hosting.Database.Interfaces
{
    public interface IDatabaseManager
    {
        IDatabase? GetDatabase(
            [Optional] string name,
            [Optional] int id
        );
    }
}
