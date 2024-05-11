// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Utilities
{
    public sealed class MonoManager : MonoSingleton<MonoManager>
    {
        public event Action<float> MonoUpdate;
        public event Action<float> MonoLateUpdate;
        public event Action MonoDestroy;

        public int UnityThreadId { get; private set; } = Thread.CurrentThread.ManagedThreadId;

        public bool InvokeNowIfMainThread = true;

        private ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();
        private bool _isDestroyed;

        protected override void Awake()
        {
            base.Awake();
            Logger.LogDebug($"unity main thread id: {UnityThreadId}");
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            MonoUpdate?.Invoke(dt);
            ProcessActionsQueue();
        }

        private void LateUpdate()
        {
            var dt = Time.unscaledDeltaTime;
            MonoLateUpdate?.Invoke(dt);
            ProcessActionsQueue();
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            // invoke to observers first. clear after invoke to observers as we are quiting
            MonoDestroy?.Invoke();
            MonoDestroy = null;
            Dispose();
        }

        private void Dispose()
        {
            var queue = _mainThreadQueue;
            var len = queue.Count;
            Logger.LogDebug($"MonoManager OnDestroy Clear actions: {len}");
            if (!queue.IsEmpty)
            {
                for (int i = 0; i < len; i++)
                {
                    var ret = queue.TryDequeue(out var _);
                    Logger.LogDebug($"remove actions queue item: #{i + 1} ret: {ret}");
                }
            }

            MonoUpdate = null;
            MonoLateUpdate = null;
            MonoDestroy = null;
            _mainThreadQueue = null;
        }

        public bool IsCurrentMainThread()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return threadId == UnityThreadId;
        }

        // ReSharper disable once IdentifierTypo
        public void EnquequeAction(Action action, bool allowInvokeNow = true)
        {
            if (_isDestroyed) return;
            if (_mainThreadQueue == null) return;
            if (action == null) return;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var isMain = threadId == UnityThreadId;
            if (allowInvokeNow && InvokeNowIfMainThread)
            {
                if (isMain)
                {
                    // Logger.LogDebug($"Invoke action from thread id: {threadId} (Main)");
                    action.Invoke();
                    return;
                }
            }

            // Logger.LogDebug($"Enqueue from thread id: {threadId}");
            _mainThreadQueue.Enqueue(action);
        }

        private void ProcessActionsQueue()
        {
            var queue = _mainThreadQueue;
            if (queue == null) return;
            if (queue.IsEmpty) return;

            var len = queue.Count;
            Logger.LogDebug($"Process actions: {len}. frame: #{Time.frameCount} (Main)");
            while (queue.TryDequeue(out Action action))
            {
                action.Invoke();
            }
        }
    }
}