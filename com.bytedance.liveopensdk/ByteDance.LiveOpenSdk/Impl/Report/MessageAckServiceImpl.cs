// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using ByteDance.LiveOpenSdk.DyCloud;
using ByteDance.LiveOpenSdk.Legacy;
using ByteDance.LiveOpenSdk.Logging;
using ByteDance.LiveOpenSdk.Room;
using Newtonsoft.Json;

namespace ByteDance.LiveOpenSdk.Report
{
    internal class MessageAckServiceImpl : IMessageAckService, IDisposable
    {
        private const string Tag = "MessageAckService";
        private readonly IDyCloudApi _dyCloudApi;
        private readonly IRoomInfoService _roomInfoService;
        private readonly AckManager _ackManager;

        public MessageAckServiceImpl(IDyCloudApi dyCloudApi, IRoomInfoService roomInfoService)
        {
            _dyCloudApi = dyCloudApi;
            _roomInfoService = roomInfoService;
            _ackManager = new AckManager
            {
                AppId = LiveOpenSdk.Env.AppId,
                Token = LiveOpenSdk.Env.Token
            };

            // FIXME
            ((DyCloudApiImpl)dyCloudApi).OnInitialize += SetupMessageListener;
            ((RoomInfoServiceImpl)roomInfoService).OnAckConfigsChanged += OnAckConfigsChanged;
        }

        public void Dispose()
        {
            _ackManager.Dispose();
        }

        private void SetupMessageListener()
        {
            Log.Debug(Tag, "SetupMessageListener");
            // 抖音云初始化后监听消息，用于履约
            _dyCloudApi.WebSocket.OnMessage += ProcessMessageAck;
        }

        private void OnAckConfigsChanged(List<AckConfig> config)
        {
            _ackManager.UpdateAckConfig(config);
        }

        private void ProcessMessageAck(string data)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<DyCloudSocketMessage>(data);
                if (message == null)
                {
                    Log.Debug(Tag, "Deserialized message is null");
                    return;
                }

                if (string.IsNullOrEmpty(message.MsgId) || string.IsNullOrEmpty(message.MsgType))
                {
                    Log.Debug(Tag, $"Message ID or type is invalid, data = {data}");
                    return;
                }

                ReportAck(AckType.Receive, message.MsgId, message.MsgType);
            }
            catch (Exception e)
            {
                Log.Warning(Tag, $"ProcessMessageAck fail, data = {data}, exception = {e}");
            }
        }

        private void ReportAck(AckType ackType, string msgId, string msgType)
        {
            Log.Debug(Tag, $"ReportAck, ackType = {ackType}, msgId = {msgId}, msgType = {msgType}");
            var roomId = _roomInfoService.RoomInfo.RoomId;
            _ackManager.ReportAck(roomId, ackType, msgId, msgType);
        }

        public void ReportAck(string msgId, string msgType)
        {
            ReportAck(AckType.Consume, msgId, msgType);
        }
    }

    internal enum AckType
    {
        Receive = 1,
        Consume = 2,
    }
}