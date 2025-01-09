using Astra.Hosting.Caching;
using Astra.Hosting.Http.Actions;
using Astra.Hosting.Http.Interfaces;
using Serilog;
using System.Net;

namespace Astra.Hosting.Http.Preprocessors.Default
{
    public sealed class HttpCacheProcessor : IHttpRequestPreprocessor
    {
        private static readonly ILogger _logger = ModuleInitialization.InitializeLogger("HttpCacheProcessor");
        
        public async Task<HttpPreprocessorContainer> TryPreprocessRequest(IHttpRequest request, IHttpResponse response)
        {
            var cacheKey = AstraHttpContext.GetCacheKey(request);
            if (HttpCacheManager.TryGet(cacheKey, out var cachedResponse))
            {
                response.Content = cachedResponse.content;
                response.ContentType = cachedResponse.contentType;
                response.SetHeader("ETag", cachedResponse.eTag);
                return new HttpPreprocessorContainer
                {
                    actionResult = Results.Configurable(HttpStatusCode.OK, cachedResponse.contentType, cachedResponse.content),
                    result = HttpPreprocessorResult.OK | HttpPreprocessorResult.STOP_AFTER
                };
            }
            
            return new HttpPreprocessorContainer
            {
                actionResult = response.ActionResult,
                result = HttpPreprocessorResult.OK
            };
        }
        
        private class HttpCachedResponse
        {
            public string MimeType { get; set; }
            public byte[] Body { get; set; }
            public string ETag { get; set; }
        }
    }
}