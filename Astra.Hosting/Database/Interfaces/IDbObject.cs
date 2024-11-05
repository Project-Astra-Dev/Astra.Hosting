using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Astra.Hosting.Database.Interfaces
{
    public interface IDbObject
    {
        ObjectId Id { get; set; }
    }
}
