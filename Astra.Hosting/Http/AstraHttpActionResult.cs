using Astra.Hosting.Http.Interfaces;
using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Astra.Hosting.Http
{
    public abstract class AstraHttpActionResult<TBodyObject> : IHttpActionResult
    {
        protected TBodyObject? BodyObject { get; }
        protected AstraHttpActionResult(TBodyObject? bodyObject = default)
        {
            BodyObject = bodyObject;
        }

        public abstract HttpStatusCode StatusCode { get; }
        public virtual string ContentType
        {
            get
            {
                if (BodyObject == null)
                    return "application/octet-stream";
                if (BodyObject.GetType().IsPrimitive())
                    return "text/plain";
                return "application/json";
            }
        }
        public virtual byte[] Body => SerializeIntoBuffer();

        protected virtual byte[] SerializeIntoBuffer()
        {
            if (BodyObject == null)
                return Array.Empty<byte>();

            if (BodyObject.GetType().IsPrimitive())
                return Encoding.UTF8.GetBytes(BodyObject.ToString() ?? "null");
            return JsonSerializer.SerializeToUtf8Bytes(BodyObject, GetSerializerOptions());
        }

        protected virtual JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreReadOnlyFields = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }
    }
}