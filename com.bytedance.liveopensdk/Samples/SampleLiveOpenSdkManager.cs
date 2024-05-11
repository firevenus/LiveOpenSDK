// Copyright (c) Bytedance. All rights reserved.
// Description:

#nullable enable
using System;
using System.Threading;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.Room;
using Douyin.LiveOpenSDK.Integration;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// 直播开放 SDK 的接入示例代码。
    ///
    /// </summary>
    public class SampleLiveOpenSdkManager
    {
        /// <summary>
        /// 请开发者修改为实际的玩法 app_id。
        /// </summary>
        public string AppId { get; set; } = "<your-app-id>";

        /// <summary>
        /// 初始化 SDK。
        /// </summary>
        /// <remarks>
        /// 请在 Unity 主线程调用。
        /// </remarks>
        public void OnCreate()
        {
            // 销毁之前创建的示例，以方便测试。
            LiveOpenSdk.Uninitialize();

            // 将 SDK 内部的日志发往 Unity 的控制台。
            LiveOpenSdk.Logger.OnLog -= UnityLogger.WriteLog;
            LiveOpenSdk.Logger.OnLog += UnityLogger.WriteLog;

            // 设置 SDK 的环境变量。
            LiveOpenSdk.Env.AppId = AppId;

            // 设置 SDK 的事件触发线程为 Unity 主线程。
            LiveOpenSdk.DefaultSynchronizationContext = SynchronizationContext.Current;

            try
            {
                // 同步初始化。
                LiveOpenSdk.Initialize();
                Debug.Log($"初始化直播开放 SDK：成功");
                SubscribeRoomInfo();
            }
            catch (Exception e)
            {
                // 正常情况下不会失败，若遇到问题，请和我们联系。
                Debug.LogError($"初始化直播开放 SDK：失败 {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 释放 SDK。通常在退出游戏或停止预览时调用。
        /// </summary>
        public void OnDestroy()
        {
            LiveOpenSdk.Uninitialize();
            OnRoomIdChanged = null;
        }

        /// <summary>
        /// 获取抖音云功能。
        /// </summary>
        public SampleDyCloudManager DyCloudManager { get; } = new SampleDyCloudManager();

        /// <summary>
        /// 订阅直播间信息。
        /// </summary>
        private void SubscribeRoomInfo()
        {
            LiveOpenSdk.RoomInfoService.OnRoomInfoChanged -= OnRoomInfoUpdate;
            LiveOpenSdk.RoomInfoService.OnRoomInfoChanged += OnRoomInfoUpdate;
        }

        /// <summary>
        /// 直播间信息的回调函数。
        /// </summary>
        /// <param name="roomInfo">最新的直播间信息</param>
        private void OnRoomInfoUpdate(IRoomInfo roomInfo)
        {
            Debug.Log($"初始化直播信息返回：成功 {roomInfo}");
            OnRoomIdChanged?.Invoke(roomInfo.RoomId);
        }


        #region FOR_TEST_SCENE

        public event Action<string>? OnRoomIdChanged;

        #endregion
    }
}