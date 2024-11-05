using Astra.Hosting.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace Astra.Hosting.Database
{
    public abstract class AstraDbObject : IDbObject
    {
        [Key] public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    }
}
