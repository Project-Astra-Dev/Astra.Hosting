using Astra.Hosting.Database.Interfaces;
using LiteDB;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Database
{
    public sealed class AstraDatabase : IDatabase
    {
        private readonly Guid _guid;
        private readonly SimpleDatabaseDescriptor _descriptor;
        private readonly ILiteDatabase _liteDatabase;
        private readonly IDatabaseBytedata _bytedata;
        private static readonly ILogger _logger = ModuleInitialization.InitializeLogger("AstraDatabase");

        public AstraDatabase(SimpleDatabaseDescriptor descriptor)
        {
            _guid = Guid.NewGuid();
            _descriptor = descriptor;
            _liteDatabase = new LiteDatabase(new ConnectionString(_descriptor.path)
            {
                Password = _descriptor.password,
                Connection = ConnectionType.Shared
            });
            _bytedata = new AstraDatabaseBytedata(this, _descriptor);
        }

        private string CreateCollectionName<T>()
        {
            var md5Hash = MD5.HashData(Encoding.UTF8.GetBytes(typeof(T).Name));
            var bitConvertedString = BitConverter.ToString(md5Hash).ToLower().Replace("-", string.Empty);

            return string.Format("{0}_{1}",
                bitConvertedString[..7], 
                typeof(T).Name.ToLower()
            );
        }

        public IDatabase DropCollection<T>() where T : AstraDbObject
        {
            var collectionName = CreateCollectionName<T>();
            if (!_bytedata.CollectionMetadata.Any(x => x.CollectionName == collectionName))
            {
                _logger.Warning("Failed to drop collection -- the database did not contain any collections for type '{Type}'", typeof(T).Name);
                return this;
            }

            _liteDatabase.DropCollection(collectionName);
            _bytedata.CollectionMetadata.RemoveAll(x => x.CollectionName == collectionName);

            _bytedata.Save();
            return this;
        }

        public ILiteCollection<T>? GetCollection<T>(bool createIfNotExisting = true) where T : AstraDbObject
        {
            var collectionName = CreateCollectionName<T>();
            if (_bytedata.CollectionMetadata.Any(x => x.CollectionName == collectionName))
                return _liteDatabase.GetCollection<T>(collectionName);

            if (createIfNotExisting)
            {
                var newCollection = _liteDatabase.GetCollection<T>(collectionName);
                _bytedata.CollectionMetadata.Add(new AstraDatabaseCollectionMetadata()
                {
                    CollectionName = collectionName,
                    CollectionType = typeof(T)?.FullName ?? "",
                    MaxItems = 100000
                });

                _bytedata.Save();
                return newCollection;
            }

            _logger.Warning("The database for type '{Type}' does not exist.", typeof(T).Name);
            return null;
        }

        public Guid GetGuid() { return _guid; }

        public SimpleDatabaseDescriptor Descriptor => _descriptor;
    }
}
