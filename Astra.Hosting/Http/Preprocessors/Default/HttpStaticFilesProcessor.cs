using Astra.Hosting.Http.Actions;
using Astra.Hosting.Http.Interfaces;
using Astra.Hosting.SDK;
using MimeTypes;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Preprocessors.Default
{
    public sealed class HttpStaticFilesProcessor : IHttpRequestPreprocessor
    {
        private static readonly ILogger _logger = ModuleInitialization.InitializeLogger("HttpStaticFilesProcessor");
        
        private readonly string _baseFolder;
        private readonly bool _allowExtensionlessLookups;
        private readonly Action<ScopedReference<byte[]>, IHttpRequest, IHttpResponse>? _onPreprocess;
        private readonly bool _stopAfterImage;
        private readonly bool _cacheImage;

        public HttpStaticFilesProcessor(
            string baseFolder,
            bool allowExtensionlessLookups = false, 
            Action<ScopedReference<byte[]>, IHttpRequest, IHttpResponse>? onPreprocess = null, 
            bool? stopAfterImage = true,
            bool? cacheImage = false)
        {
            _baseFolder = baseFolder;
            _allowExtensionlessLookups = allowExtensionlessLookups;
            _onPreprocess = onPreprocess;
            _stopAfterImage = stopAfterImage.HasValue && stopAfterImage.Value;
            _cacheImage = cacheImage.HasValue && cacheImage.Value;
        }

        public async Task<HttpPreprocessorContainer> TryPreprocessRequest(IHttpRequest request, IHttpResponse response)
        {
            string filePath = GetFilePath(request.Uri);
            
            if (await FileExistsAsync(filePath))
                return await CreateSuccessResponseAsync(filePath, request, response);

            if (await DirectoryExistsAsync(filePath))
            {
                string indexFile = await FindIndexFileAsync(filePath);
                if (indexFile != null)
                    return await CreateSuccessResponseAsync(indexFile, request, response);
            }

            if (_allowExtensionlessLookups)
            {
                string fileWithExtension = await FindFileWithExtensionAsync(filePath);
                if (fileWithExtension != null)
                    return await CreateSuccessResponseAsync(fileWithExtension, request, response);
            }

            return new HttpPreprocessorContainer
            {
                actionResult = Results.NotFound(),
                result = HttpPreprocessorResult.FAIL
            };
        }

        private string GetFilePath(string uri)
        {
            string[] parts = new[] { Environment.CurrentDirectory, _baseFolder }
                .Concat(uri.Split('/', StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
            return Path.Combine(parts);
        }

#pragma warning disable CS8603 // Possible null reference return.
        private async Task<string> FindIndexFileAsync(string directoryPath)
        {
            var files = await Task.Run(() => Directory.GetFiles(directoryPath));
            return files.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Equals("index", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<string> FindFileWithExtensionAsync(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (directory == null) return null;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            var files = await Task.Run(() => Directory.GetFiles(directory));
            return files.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));
#pragma warning restore CS8603 // Possible null reference return.
        }

        private async Task<HttpPreprocessorContainer> CreateSuccessResponseAsync(string filePath, IHttpRequest request, IHttpResponse response)
        {
            string mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(filePath).TrimStart('.'));
            byte[] fileContent = await File.ReadAllBytesAsync(filePath);

            var scopedReference = ScopedReference<byte[]>.New(ref fileContent);
            _onPreprocess?.Invoke(scopedReference, request, response);

            return new HttpPreprocessorContainer
            {
                actionResult = Results.Configurable(HttpStatusCode.OK, mimeType, scopedReference.GetValue()),
                result = _stopAfterImage 
                    ? _cacheImage
                        ? HttpPreprocessorResult.OK | HttpPreprocessorResult.CACHE | HttpPreprocessorResult.STOP_AFTER
                        : HttpPreprocessorResult.OK | HttpPreprocessorResult.STOP_AFTER
                    : HttpPreprocessorResult.OK
            };
        }

        private Task<bool> FileExistsAsync(string path) => Task.Run(() => File.Exists(path));
        private Task<bool> DirectoryExistsAsync(string path) => Task.Run(() => Directory.Exists(path));
    }
}