// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using Douyin.LiveOpenSDK.Data;
using Douyin.LiveOpenSDK.Utilities;

namespace Douyin.LiveOpenSDK.Modules
{
    /// 实现履约Ack的批次和定时，对应一种消息类型、ack类型
    internal class AckLocalState
    {
        public bool IsEnabled { get; set; }
        public string msg_type { get; private set; }
        public int ack_type { get; private set; }
        public string TypeKey { get; private set; }

        // key类型（msg_type和ack_type 的组合）
        internal static string MakeTypeKey(string msg_type, long ack_type) => $"{msg_type}_{ack_type}";

        private AckConfig _ackConfig;
        private readonly List<MsgAckItem> _messages = new List<MsgAckItem>();
        private long _nowTimestampMs;
        private long _waitTimestampMs;

        /// 构造 config 对应的 State
        public AckLocalState(AckConfig ackConfig)
        {
            _ackConfig = ackConfig;
            msg_type = _ackConfig.msg_type;
            ack_type = (int) _ackConfig.ack_type;
            TypeKey = MakeTypeKey(_ackConfig.msg_type, _ackConfig.ack_type);
            IsEnabled = true;
        }

        /// 构造空的 State, IsEnabled = false
        public AckLocalState(string msg_type, int ack_type)
        {
            this.msg_type = msg_type;
            this.ack_type = ack_type;
            TypeKey = MakeTypeKey(msg_type, ack_type);
        }

        /// 更新 config
        public void UpdateConfig(AckConfig ackConfig)
        {
            _ackConfig = ackConfig;
            IsEnabled = true;
        }

        // 6. 塞入待发送的消息队列
        // 7. 检查队列中消息数量，是否数量0增加到>0，若是，标记本地Ack API状态的批次等待时间戳为当前时间
        public void PushMessage(MsgAckItem msg)
        {
            if (_messages.Count == 0)
                _nowTimestampMs = _waitTimestampMs = TimeUtil.NowTimestampMs;
            _messages.Add(msg);
        }

        // 8.1.立即将这些消息调用履约上报Ack API进行上报
        // 8.2.重置批次等待时间戳归零 =0
        public List<MsgAckItem> DequeueMessages()
        {
            var retMessages = new List<MsgAckItem>(_messages);
            _messages.Clear();
            _waitTimestampMs = 0;
            return retMessages;
        }

        /// 队列中消息数量，是否 ≥ 大于等于Ack批次配置中的 "batch_max_num" 数量
        public bool IsReachedMessagesCount()
        {
            return _messages.Count >= _ackConfig.batch_max_num;
        }

        public int GetMessagesCount() => _messages.Count;

        /// 消息已等待的时间（即：当前时间戳，减去最后批次上报的时间戳）是否 ≥ 大于等于Ack批次配置中的 "batch_interval" 的时长
        public bool IsReachedWaitedTime()
        {
            if (_waitTimestampMs == 0)
                return false;

            var intervalMs = _ackConfig.batch_interval * 1000;
            var waitedMs = GetWaitedTimeMs();
            return waitedMs >= intervalMs;
        }

        public float GetWaitedTimeSec()
        {
            if (_waitTimestampMs != 0)
                return GetWaitedTimeMs() / 1000.0f;
            return 0;
        }

        public long GetWaitedTimeMs()
        {
            _nowTimestampMs = TimeUtil.NowTimestampMs;
            return _nowTimestampMs - _waitTimestampMs;
        }
    }

    // 实现履约AckConfig对应功能： 批次和定时
    internal class AckConfigsModule
    {
        public string room_id { get; set; }

        public Dictionary<string, AckLocalState> LocalAckStates => _localAckStates;

        // key: {msg_type}_{ack_type}
        private readonly Dictionary<string, AckLocalState> _localAckStates = new Dictionary<string, AckLocalState>();

        protected static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        public void UpdateTime(float time)
        {
        }

        public void PushMessages(string roomId, int ack_type, List<MsgAckItem> msgList)
        {
            room_id = roomId;
            foreach (var msg in msgList)
            {
                var state = GetLocalState(msg.msg_type, ack_type);
                if (state != null)
                    state.PushMessage(msg);
            }
        }

        public void ParseAndSetAckConfigs(string callID, List<AckConfig> ack_cfg_list)
        {
            int i = 0;

            var existingKeys = new List<string>(_localAckStates.Keys);
            var newKeys = new List<string>();
            foreach (var config in ack_cfg_list)
            {
                i++;
                Debug.LogDebug($"{callID} ackConfig #{i}: {SdkUtils.ToJsonString(config)}");
                var typeKey = AckLocalState.MakeTypeKey(config.msg_type, config.ack_type);
                newKeys.Add(typeKey);
                SetConfig(typeKey, config);
            }

            foreach (var key in existingKeys)
            {
                if (!newKeys.Contains(key))
                    DisableConfig(key);
            }
        }

        private AckLocalState GetLocalState(string msg_type, int ack_type)
        {
            var key = AckLocalState.MakeTypeKey(msg_type, ack_type);
            if (_localAckStates.TryGetValue(key, out var existingConfig))
            {
                return existingConfig;
            }

            // 礼物数据勿丢弃
            if (msg_type == LiveMsgType.live_gift)
            {
                var state = _localAckStates[key] = new AckLocalState(msg_type, ack_type);
                return state;
            }

            return null;
        }

        private void SetConfig(string key, AckConfig config)
        {
            if (_localAckStates.TryGetValue(key, out var existingConfig))
            {
                existingConfig.UpdateConfig(config);
            }
            else
            {
                _localAckStates[key] = new AckLocalState(config);
            }
        }

        private void DisableConfig(string key)
        {
            _localAckStates[key].IsEnabled = false;
        }
    }
}