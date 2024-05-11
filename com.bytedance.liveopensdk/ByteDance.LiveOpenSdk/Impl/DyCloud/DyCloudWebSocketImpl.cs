// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Logging;
using dyCloudUnitySDK;

namespace ByteDance.LiveOpenSdk.DyCloud
{
    internal class DyCloudWebSocketImpl : IDyCloudWebSocket, IDisposable
    {
        private const string Tag = "DyCloudWebSocket";
        private const string ErrorCodeClosedByUser = "4998";
        private readonly DYCloud _dyCloud;
        private readonly CancellationTokenSource _cts;
        private int _stateVal;
        private DYCloudWebSocket? _dyCloudWs;
        private TaskCompletionSource<object?>? _connectTcs; // non-generic version requires .NET 5

        public DyCloudWebSocketImpl(DYCloud dyCloud)
        {
            _dyCloud = dyCloud;
            _cts = new CancellationTokenSource();
            SetState(State.Closed);
        }

        public void Dispose()
        {
            Close();
            SetState(State.Disposed);
            _cts.Dispose();
            _connectTcs?.TrySetCanceled();
            _connectTcs = null;
            OnOpen = null;
            OnClose = null;
            OnMessage = null;
            OnError = null;
            Log.Verbose(Tag, "Disposed");
        }

        public DyCloudWebSocketOptions Options { get; } = new DyCloudWebSocketOptions();

        public Task ConnectContainerAsync(string path)
        {
            return ConnectContainerAsync(path, "");
        }

        public async Task ConnectContainerAsync(string path, string serviceId)
        {
            MoveState(State.Closed, State.Connecting);
            Log.Debug(Tag, $"ConnectContainer, path = {path}, serviceId = {serviceId}");
            try
            {
                _connectTcs ??= new TaskCompletionSource<object?>();
                _dyCloudWs = await Task.Run(() => _dyCloud.connectContainer(
                    path,
                    serviceId,
                    ProcessOnOpen,
                    ProcessOnClose,
                    ProcessOnMessage,
                    ProcessOnError,
                    Options.AutoReconnect
                ), _cts.Token);
                await _connectTcs.Task;
                Log.Debug(Tag, $"ConnectContainer complete, path = {path}, serviceId = {serviceId}");
            }
            catch (Exception e)
            {
                Log.Debug(Tag,
                    $"ConnectContainer fail, path = {path}, serviceId = {serviceId}, exception = {e}");
                MoveState(State.Connecting, State.Closed);
                throw;
            }
        }

        public void SendMessage(string message)
        {
            var ws = _dyCloudWs ?? throw new NullReferenceException("WebSocket is null");
            Log.Debug(Tag, $"SendMessage, message = {message}");
            ws.SendMessage(message);
        }

        public void Close()
        {
            if (_dyCloudWs == null) return;
            Log.Debug(Tag, $"Close");
            _dyCloudWs?.Close();
            _dyCloudWs = null;
        }

        public event OnOpenCallback? OnOpen;
        public event OnCloseCallback? OnClose;
        public event OnMessageCallback? OnMessage;
        public event OnErrorCallback? OnError;

        private void ProcessOnOpen()
        {
            Log.Verbose(Tag, "ProcessOnOpen");
            TryMoveState(State.Closed, State.Connected);
            TryMoveState(State.Connecting, State.Connected);
            LiveOpenSdk.Post(() => OnOpen?.Invoke());

            _connectTcs?.TrySetResult(null);
            _connectTcs = null;
        }

        private void ProcessOnClose()
        {
            Log.Verbose(Tag, "ProcessOnClose");
            TryMoveState(State.Connecting, State.Closed);
            TryMoveState(State.Connected, State.Closed);
            LiveOpenSdk.Post(() => OnClose?.Invoke());

            _connectTcs?.TrySetCanceled();
            _connectTcs = null;
        }

        private void ProcessOnMessage(string data)
        {
            Log.Verbose(Tag, $"ProcessOnMessage, data = {data}");
            if (IsState(State.Connecting) || IsState(State.Connected))
            {
                LiveOpenSdk.Post(() => OnMessage?.Invoke(data));
            }
        }

        private void ProcessOnError(string errorMsg)
        {
            Log.Verbose(Tag, $"ProcessOnError, errorMsg = {errorMsg}");
            var error = new DyCloudWebSocketError(errorMsg);
            if (ShouldIgnoreError(error)) return;
            LiveOpenSdk.Post(() => OnError?.Invoke(error));
        }

        private bool ShouldIgnoreError(IDyCloudWebSocketError error)
        {
            return error.Code == ErrorCodeClosedByUser;
        }

        private enum State
        {
            Closed,
            Connecting,
            Connected,
            Disposed
        }

        private void SetState(State toState)
        {
            Interlocked.Exchange(ref _stateVal, (int)toState);
        }

        private bool IsState(State state)
        {
            return Interlocked.CompareExchange(ref _stateVal, 0, 0) == (int)state;
        }

        private void MoveState(State fromState, State toState)
        {
            var oldState = CompareExchangeState(fromState, toState);
            if (oldState != (int)fromState)
            {
                throw new InvalidOperationException($"Required state is {fromState}, got {(State)oldState}");
            }
        }

        private bool TryMoveState(State fromState, State toState)
        {
            var oldState = CompareExchangeState(fromState, toState);
            return oldState == (int)fromState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CompareExchangeState(State fromState, State toState)
        {
            var oldState = Interlocked.CompareExchange(ref _stateVal, (int)toState, (int)fromState);
            return oldState;
        }
    }
}