// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Logging;
using dyCloudUnitySDK;

namespace ByteDance.LiveOpenSdk.DyCloud
{
    internal class DyCloudApiImpl : IDyCloudApi, IDisposable
    {
        private const string Tag = "DyCloudApi";

        private const string DebugToken =
            "DEBUFG7yPFcLy0qa75gtYst4jCsGwvT9I3fvVnKuYHU3PQ39+sG09bSz9OVQU6Kw2RDnak2jT9kFDVBSaY0wVOxMozvX1YsZ7YEmaPbKCLlAf1haorLmSfG14hWI=";

        private DYCloud? _dyCloud;
        private DyCloudWebSocketImpl? _webSocket;

        public DyCloudApiImpl()
        {
        }

        public event Action? OnInitialize;

        public async Task InitializeAsync(DyCloudInitParams initParams)
        {
            Log.Info(Tag, $"Initialize, {initParams}");
            try
            {
                await Task.Run(() => Initialize(initParams));
                Log.Info(Tag, $"Initialize complete");
            }
            catch (Exception e)
            {
                Log.Error(Tag, $"Initialize fail, exception = {e}");
                throw;
            }
        }

        public async Task<IDyCloudHttpResponse> CallContainerAsync(string path, string serviceId, string method,
            string body,
            IDictionary<string, string> headers)
        {
            Log.Debug(Tag, $"CallContainer, path = {path}, serviceId = {serviceId}, method = {method}");
            try
            {
                var result = await Task.Run(() => CallContainer(path, serviceId, method, body, headers));
                Log.Debug(Tag, $"CallContainer complete, path = {path}, serviceId = {serviceId}, method = {method}");
                return result;
            }
            catch (Exception e)
            {
                Log.Error(Tag,
                    $"CallContainer fail, path = {path}, serviceId = {serviceId}, method = {method}, exception = {e}");
                throw;
            }
        }

        public IDyCloudWebSocket WebSocket =>
            _webSocket ?? throw new InvalidOperationException("DyCloud is not initialized");

        private void PopulateMissingProperties(DyCloudInitParams initParams)
        {
            var env = LiveOpenSdk.Env;
            // 从全局公共参数读取token
            if (string.IsNullOrEmpty(initParams.Token))
            {
                initParams.Token = env.Token;
            }

            // 调试模式且token为空时，使用内置的调试token
            if (string.IsNullOrEmpty(initParams.Token) && initParams.IsDebug)
            {
                initParams.Token = DebugToken;
            }

            if (string.IsNullOrEmpty(initParams.AppId))
            {
                initParams.AppId = env.AppId;
            }
        }

        private void Initialize(DyCloudInitParams initParams)
        {
            if (_dyCloud != null)
            {
                throw new InvalidOperationException("DyCloud is already initialized");
            }

            PopulateMissingProperties(initParams);
            Log.Debug(Tag, $"Initialize, final init params = {initParams}");

            if (string.IsNullOrEmpty(initParams.Token))
            {
                throw new ArgumentException($"{nameof(DyCloudInitParams.Token)} is empty");
            }

            if (string.IsNullOrEmpty(initParams.AppId))
            {
                throw new ArgumentException($"{nameof(DyCloudInitParams.AppId)} is empty");
            }

            _dyCloud = new DYCloud(
                initParams.Token,
                initParams.AppId,
                initParams.EnvId,
                initParams.DefaultServiceId,
                initParams.IsDebug,
                initParams.DebugIpAddress
            );
            _webSocket = new DyCloudWebSocketImpl(_dyCloud);
            OnInitialize?.Invoke();
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _dyCloud?.destroy();
            _dyCloud = null;
            OnInitialize = null;
            Log.Verbose(Tag, "Disposed");
        }

        private IDyCloudHttpResponse CallContainer(string path, string serviceId, string method, string body,
            IDictionary<string, string> headers)
        {
            var dyCloud = _dyCloud ?? throw new InvalidOperationException("DyCloud is not initialized");
            var headerDict = new Dictionary<string, string>(headers);
            var resp = dyCloud.callContainer(path, serviceId, method, headerDict, body);
            return new DyCloudHttpResponseAdapter(resp);
        }

        private class DyCloudHttpResponseAdapter : IDyCloudHttpResponse
        {
            private readonly DYCloudHttpResponse _original;

            public DyCloudHttpResponseAdapter(DYCloudHttpResponse original)
            {
                _original = original;
            }

            public int StatusCode => _original.statusCode;
            public string Body => _original.body;
            public IReadOnlyDictionary<string, string> Headers => _original.headers;
        }
    }
}