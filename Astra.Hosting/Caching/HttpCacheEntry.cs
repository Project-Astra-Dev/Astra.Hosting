namespace Astra.Hosting.Caching;

public readonly struct HttpCacheEntry
{
    public HttpCacheEntry(string id, string contentType, string eTag, byte[] content, DateTime expires)
    {
        this.id = id;
        this.contentType = contentType;
        this.eTag = eTag;
        this.content = content;
        this.expiry = expires;
    }
    
    public readonly string id;
    public readonly string contentType;
    public readonly string eTag;
    public readonly byte[] content;
    public readonly DateTime expiry;
}