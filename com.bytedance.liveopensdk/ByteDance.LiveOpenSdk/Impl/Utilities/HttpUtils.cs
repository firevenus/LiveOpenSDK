// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Logging;
using Newtonsoft.Json;

namespace ByteDance.LiveOpenSdk.Utilities
{
    internal static class HttpUtils
    {
        private const string Tag = "HttpUtils";
        private const string MimeTypeApplicationJson = "application/json";
        public static HttpClient HttpClient { get; }

        static HttpUtils()
        {
            var httpClientHandler = new HttpClientHandler();
            // 某些Unity环境下的HttpClient默认不走系统代理，这里配置一下
            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = WebRequest.DefaultWebProxy;
            HttpClient = new HttpClient(httpClientHandler);
        }

        public static async Task<TResponse> Post<TResponse>(
            string requestUri,
            IDictionary<string, string> headers,
            object body,
            CancellationToken cancellationToken
        )
        {
            var serializedBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(
                serializedBody,
                Encoding.UTF8,
                MimeTypeApplicationJson
            );

            // 添加公共参数
            content.Headers.AddEnvHeaders();
            foreach (var header in headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }

            Log.Verbose(Tag, $"Post, uri = {requestUri}, headers = {content.Headers}, content = {serializedBody}");

            var response = await HttpClient.PostAsync(requestUri, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var decodedResponse = JsonConvert.DeserializeObject<TResponse>(responseString)!;
            Log.Verbose(Tag, $"Post, uri = {requestUri}, response = {decodedResponse}");
            return decodedResponse;
        }

        private static void AddEnvHeaders(this HttpHeaders headers)
        {
            foreach (var entry in LiveOpenSdk.Env.HttpHeaders)
            {
                headers.Add(entry.Key, entry.Value);
            }
        }
    }
}