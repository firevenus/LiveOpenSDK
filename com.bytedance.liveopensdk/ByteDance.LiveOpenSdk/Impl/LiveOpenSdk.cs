// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Threading;
using ByteDance.LiveOpenSdk.DyCloud;
using ByteDance.LiveOpenSdk.Logging;
using ByteDance.LiveOpenSdk.Report;
using ByteDance.LiveOpenSdk.Room;
using ByteDance.LiveOpenSdk.Utilities;

namespace ByteDance.LiveOpenSdk
{
    /// <summary>
    /// 直播开放 SDK 的顶层类。
    /// </summary>
    public static class LiveOpenSdk
    {
        private static readonly object _lock = new object();
        private static bool _isInitialized;
        private static readonly ServiceCollection _sc = new ServiceCollection();

        /// <summary>
        /// SDK 的日志输出。
        /// </summary>
        public static ILogger Logger { get; } = new LiveOpenSdkLogger();

        /// <summary>
        /// 环境公共参数。
        /// </summary>
        /// <remarks>
        /// 请在初始化 SDK 之前设置好相关参数。
        /// </remarks>
        public static LiveOpenSdkEnv Env { get; } = new LiveOpenSdkEnv();

        /// <summary>
        /// 回调事件的同步上下文。<br/>
        /// 若希望事件在特定的线程触发，可以设置这个属性。
        /// </summary>
        public static SynchronizationContext? DefaultSynchronizationContext { get; set; } = null;

        public static IDyCloudApi DyCloudApi => GetService<IDyCloudApi>();

        public static IRoomInfoService RoomInfoService => GetService<IRoomInfoService>();

        public static IMessageAckService MessageAckService => GetService<IMessageAckService>();

        /// <summary>
        /// 初始化 SDK。<br/>
        /// 在初始化成功后才能调用 SDK 内部的服务。
        /// </summary>
        /// <remarks>
        /// 重复初始化没有效果。<br/>
        /// 初始化过程中若发生错误，会抛出异常。
        /// </remarks>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    // 自动填充 Token
                    if (string.IsNullOrEmpty(Env.Token))
                    {
                        Env.Token = CommandLineUtils.LaunchToken;
                    }

                    InitializeImpl();
                    _isInitialized = true;
                    Log.Info(nameof(LiveOpenSdk), $"Initialize complete");
                }
                catch (Exception e)
                {
                    Log.Error(nameof(LiveOpenSdk), $"Initialize fail, exception = {e}");
                    _sc.Dispose();
                    throw;
                }
            }
        }

        /// <summary>
        /// 反初始化 SDK，停止并销毁内部运行的服务。
        /// </summary>
        public static void Uninitialize()
        {
            lock (_lock)
            {
                if (!_isInitialized) return;
                _isInitialized = false;
                _sc.Dispose();
                Log.Info(nameof(LiveOpenSdk), $"Uninitialize complete");
            }
        }

        /// <summary>
        /// 获取 SDK 内的服务。<br/>
        /// 可获取的服务的接口上带有 <see cref="ServiceApiAttribute"/> 特性。
        /// </summary>
        /// <typeparam name="T">服务的接口类型</typeparam>
        /// <returns>服务对象</returns>
        public static T GetService<T>()
        {
            lock (_lock)
            {
                return _sc.Get<T>();
            }
        }

        private static void InitializeImpl()
        {
            var dyCloudApi = new DyCloudApiImpl();
            var roomInfoService = new RoomInfoServiceImpl();
            var messageAckService = new MessageAckServiceImpl(dyCloudApi, roomInfoService);
            _sc.Add(typeof(IDyCloudApi), dyCloudApi);
            _sc.Add(typeof(IRoomInfoService), roomInfoService);
            _sc.Add(typeof(IMessageAckService), messageAckService);
        }

        internal static void Post(Action action)
        {
            var ctx = DefaultSynchronizationContext;
            if (ctx != null)
            {
                ctx.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
    }

    internal class ServiceCollection : IDisposable
    {
        private readonly Dictionary<Type, object> _dict = new Dictionary<Type, object>();

        public void Add(Type key, object value)
        {
            _dict[key] = value;
        }

        public T Get<T>()
        {
            _dict.TryGetValue(typeof(T), out var value);
            return (T)value;
        }

        public void Dispose()
        {
            foreach (var obj in _dict.Values)
            {
                if (!(obj is IDisposable d)) continue;
                try
                {
                    d.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error(nameof(LiveOpenSdk), e.ToString());
                }
            }

            _dict.Clear();
        }
    }

    internal class LiveOpenSdkLogger : ILogger
    {
        public event Action<LogItem>? OnLog;

        public void WriteLog(LogItem item)
        {
            OnLog?.Invoke(item);
        }
    }
}