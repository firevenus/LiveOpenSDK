// Copyright@www.bytedance.com
// Author: liziang
// Date: 2024/02/18
// Description:

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal static class HeaderDefined
    {
        public const string TtLogId = "X-Tt-Logid";
        public const string ContentType = "content-type";
    }

    /// <summary>
    /// http调用
    /// </summary>
    /// <remarks>`HttpHelper` and its used `UnityWebRequest` require main thread!</remarks>
    /// <remarks>`HttpHelper`及其使用的`UnityWebRequest`必须要在主线程!</remarks>
    internal class HttpHelper : IDisposable
    {
        public readonly string host;
        public readonly string path;
        public Dictionary<string, string> headers { get; protected set; }
        public int Timeout { get; set; } = 30;
        public LogLevel LevelForReqFail { get; set; } = LogLevel.Error;

        public string Url { get; protected set; }

        private SdkDebugLogger Debug => LiveOpenSDK.Debug;
        protected MonoManager MonoManager => MonoManager.GetInstance();

        private static string CombineUrl(string host, string path)
        {
            var sep1 = host.EndsWith("/");
            var sep2 = path.StartsWith("/");
            if (sep1 && sep2)
                return host.Substring(0, host.Length - 1) + path;
            if (!sep1 && !sep2)
                return $"{host}/{path}";
            return host + path;
        }


        public HttpHelper(string host, string path)
        {
            if (string.IsNullOrEmpty(host))
                Debug.LogError($"arg `{nameof(host)}` is empty!");
            if (string.IsNullOrEmpty(path))
                Debug.LogError($"arg `{nameof(path)}` is empty!");
            this.host = host ?? "";
            this.path = path ?? "";
            Url = CombineUrl(this.host, this.path);
            SetHeader(HeaderDefined.ContentType, "application/json");
            SdkUtils.SetHeadersEnv(headers);
        }

        public void SetHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                Debug.LogError("arg `key` is empty!");
            if (null == headers)
                headers = new Dictionary<string, string>();
            key = key ?? "";
            headers[key] = value ?? "";
        }

        // ReSharper disable once UnusedMember.Local
        private async Task<HttpAPIResponse> RequestMethod()
        {
            // note: 调试用 only for IDE to find this `HttpAPIResponse` ctor usage, because the next method is generic
            var resp = new HttpAPIResponse();
            await Task.Yield();
            return resp;
        }

        /// note: `HttpHelper` and its used `UnityWebRequest` require main thread!
        public async Task<TResponse> RequestMethod<TResponse>(string httpType, string body, RespSourceType respSource = RespSourceType.Unknown)
            where TResponse : HttpAPIResponse, new()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var resp = new TResponse();
            resp.path = path;
            resp.httpType = httpType;
            resp.respSource = respSource;
            try
            {
                if (!MonoManager.IsCurrentMainThread())
                    Debug.LogError("`HttpHelper` and `UnityWebRequest` require main thread! 必须要在主线程调用！");

                // ReSharper disable once ConvertToUsingDeclaration
                using (var req = new UnityWebRequest(Url))
                {
                    req.method = httpType;
                    if (headers != null)
                    {
                        foreach (var pair in headers)
                        {
                            req.SetRequestHeader(pair.Key, pair.Value);
                        }
                    }

                    body = body ?? "";
                    if (!string.IsNullOrEmpty(body))
                    {
                        var bytes = new System.Text.UTF8Encoding().GetBytes(body);
                        req.uploadHandler = new UploadHandlerRaw(bytes);
                    }

                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.timeout = Timeout;

                    Debug.LogDebug($"http req {httpType} url: {Url}, headers: {SdkUtils.ToJsonString(headers)}, body: {body}");
                    req.SendWebRequest();

                    while (!req.isDone)
                    {
                        await Task.Yield();
                    }

                    resp.AcceptWebRequest(req);

                    var resultType = resp.reqResultType;
                    var errorMsg = resp.errorMsg;
                    var isError = resultType != UnityWebRequest.Result.Success;
                    isError = isError || !string.IsNullOrEmpty(errorMsg);
                    if (isError)
                    {
                        OnRequestError($"http error! {resultType}, {httpType} {path} {resp.statusCode}, \"{errorMsg}\"", resp);
                    }
                    else
                    {
                        Debug.Log($"http {resultType}, {resp.ToRespInfo()}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                resp.respSource = RespSourceType.SDKClient;
                resp.AcceptException(e);
                OnRequestError($"http exception! exception: {e.GetType()} {e.Message}", resp);
            }

            return resp;
        }

        private void OnRequestError(string error, HttpAPIResponse resp)
        {
            var level = LevelForReqFail;
            error = Debug.ColorText(error, level);
            var errorLog = $"{error} {resp.ToRespInfo()}";
            if (level == LogLevel.Error)
                Debug.LogError(errorLog);
            else
                Debug.LogByLevel(level, errorLog);
        }

        public void Dispose()
        {
        }
    }
}