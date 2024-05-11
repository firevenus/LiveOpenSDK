// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Legacy;
using ByteDance.LiveOpenSdk.Logging;
using ByteDance.LiveOpenSdk.Utilities;

namespace ByteDance.LiveOpenSdk.Report
{
    /// <summary>
    /// 履约上报管理器。移植自老代码，待重构。
    /// </summary>
    internal class AckManager : IDisposable
    {
        private const string Tag = "AckManager";
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly AckConfigsModule _ackConfigs = new AckConfigsModule();

        public string AppId { get; set; } = "";
        public string Token { get; set; } = "";

        public AckManager()
        {
            Task.Run(RunUpdateLoop);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        private async Task RunUpdateLoop()
        {
            // 原先是每帧处理，现在粗略启动一个定时循环来处理
            var cancellationToken = _cts.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _ProcessPushedAckMessages();
                }
                catch (Exception e)
                {
                    Log.Warning(Tag, $"RunUpdateLoop, exception = {e}");
                }

                await Task.Delay(UpdateInterval, cancellationToken);
            }
        }

        public void UpdateAckConfig(List<AckConfig> configList)
        {
            Log.Debug(Tag, "UpdateAckConfig");
            _ackConfigs.ParseAndSetAckConfigs("UpdateAckConfigInternal#1", configList);
        }

        public void ReportAck(string roomId, AckType ackType, string msgId, string msgType)
        {
            var item = new MsgAckItem(msgId, msgType);
            ReportAck(roomId, ackType, item);
        }


        private void ReportAck(string roomId, AckType ackType, MsgAckItem item)
        {
            // 前置检查参数
            if (string.IsNullOrEmpty(roomId))
            {
                Log.Warning(Tag, "ReportAck warning, roomId is empty");
            }

            var errorField = MsgAckItem.ValidateErrorField(item);
            if (!string.IsNullOrEmpty(errorField))
            {
                Log.Warning(Tag, $"ReportAck warning, {errorField}");
            }

            // 内部非线程安全，调度到主线程处理
            LiveOpenSdk.Post(() => { _ackConfigs.PushMessages(roomId, (int)ackType, new List<MsgAckItem> { item }); });
        }

        private async Task _ProcessPushedAckMessages()
        {
            foreach (var pair in _ackConfigs.LocalAckStates)
            {
                var localState = pair.Value;
                if (!localState.IsEnabled)
                    continue;
                if (localState.GetMessagesCount() == 0)
                    continue;

                var typeKey = localState.TypeKey;
                var ack_type = localState.ack_type;
                var isReachCount = localState.IsReachedMessagesCount();
                var isReachTime = localState.IsReachedWaitedTime();
                var count = localState.GetMessagesCount();
                var time = localState.GetWaitedTimeSec();
                if (!isReachCount && !isReachTime)
                    continue;

                var room_id = _ackConfigs.room_id;
                var sendMsgList = localState.DequeueMessages();
                if (isReachCount)
                    Log.Info(Tag, $"AckMessages 履约 {typeKey} 满足 waited count: {count} reach, waited time: {time:F3}s");
                if (isReachTime)
                    Log.Info(Tag, $"AckMessages 履约 {typeKey} 满足 waited time: {time:F3}s reach, waited count: {count}");
                if (sendMsgList != null && sendMsgList.Count > 0)
                {
                    // 不等待了
                    _ = Task.Run(() => SendAckRequest(room_id, ack_type, sendMsgList));
                }
            }
        }

        private async Task SendAckRequest(string roomId, int ackType, List<MsgAckItem> itemList)
        {
            try
            {
                var msgListDataStr = new AckReqMsgListConverter(itemList).ToJsonString();

                var body = new AckAPIReq()
                {
                    room_id = roomId,
                    ack_type = ackType,
                    app_id = AppId,
                    data = msgListDataStr,
                };

                var uriBuilder = new UriBuilder
                {
                    Scheme = "https",
                    Host = ConstsInternal.ApiHost_Webcast,
                    Path = ConstsInternal.ApiPath_Ack
                };
                var requestUri = uriBuilder.ToString();

                var headers = new Dictionary<string, string>
                {
                    ["token"] = Token
                };

                var response = await HttpUtils.Post<object>(
                    requestUri,
                    headers,
                    body,
                    _cts.Token
                );
                Log.Debug(Tag, $"SendAckRequest, response = {response}");
            }
            catch (Exception e)
            {
                Log.Error(Tag, $"SendAckRequest fail, exception = {e}");
            }
        }
    }
}