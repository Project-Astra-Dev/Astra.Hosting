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
        OK = 0,
        FAIL = 1,
        STOP_AFTER = 2
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
