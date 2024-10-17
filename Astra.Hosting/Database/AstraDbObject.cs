using Astra.Hosting.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Database
{
    public abstract class AstraDbObject : IDbObject
    {
        private string _objectId = "";
        public string ObjectId
        {
            get
            {
                if (string.IsNullOrEmpty(_objectId))
                {
                    var md5Hash = MD5.HashData(Encoding.UTF8.GetBytes(GetType().Name));
                    var bitConvertedString = BitConverter.ToString(md5Hash).ToLower().Replace("-", string.Empty);
                    _objectId = bitConvertedString[..7];
                }

                return _objectId;
            }
        }
    }
}
