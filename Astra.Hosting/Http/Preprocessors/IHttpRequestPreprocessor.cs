using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Preprocessors
{
    [Flags]
    public enum HttpPreprocessorResult
    {
        // Tells the server that the preprocessor executed
        // successfully.
        OK = 0,
        // Tells the server to stop processing the request
        // and return an error message sent by the preprocessor
        FAIL = 1,
        // Tells the server to stop processing the request
        // and return whatevers been sent by the preprocessor
        STOP_AFTER = 2,
        // Tells the server to cache the response in 
        // HttpCacheManager
        CACHE = 4,
    }

    public struct HttpPreprocessorContainer
    {
        public IHttpActionResult actionResult;
        public HttpPreprocessorResult result;
    }

    public interface IHttpRequestPreprocessor
    {
        Task<HttpPreprocessorContainer> TryPreprocessRequest(IHttpRequest request, IHttpResponse response);
    }
}
