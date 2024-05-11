// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace ByteDance.LiveOpenSdk.Legacy
{
    /// <summary>
    /// 直播互动消息数据。 一个数据对象，对应一条互动消息：评论、礼物、或点赞等。
    /// </summary>
    [Serializable]
    [DataContract]
    public class LiveMsgItem
    {
        /// 缓存透传的原始数据。 只用作本地缓存和读取，不用作API请求参数字段。
        public string data_cached { get; set; }

        /// 唯一标识，平台推送payload数据里的msg_id。会被用于履约校验
        [DataMember(Name = "msg_id")] public string msg_id;

        /// 消息类型，
        /// 1. 评论：live_comment
        /// 2. 礼物：live_gift
        /// 3. 点赞：live_like
        /// <see cref="LiveMsgType"/>
        [DataMember(Name = "msg_type")] public string msg_type;

        /// 毫秒级时间戳，例如 1705989099973。当ack_type为1时即为指令收到后的时间，当ack_type为2时即为渲染成功后的时间
        [DataMember(Name = "client_time")] public long client_time;

        /// 一条消息数据，使用当前时间戳
        /// <param name="rawData">透传原始数据</param>
        /// <param name="msg_id">消息id，见成员：<see cref="MsgAckItem.msg_id"/></param>
        /// <param name="msg_type">消息类型，见成员：<see cref="MsgAckItem.msg_type"/></param>
        public LiveMsgItem(string rawData, string msg_id, string msg_type)
        {
            data_cached = rawData ?? "";
            this.msg_id = msg_id ?? "";
            this.msg_type = msg_type ?? "";
            UseNowTimestamp();
        }

        /// 一条消息数据，使用指定时间戳
        /// <param name="rawData">透传原始数据</param>
        /// <param name="msg_id">消息id，见成员：<see cref="MsgAckItem.msg_id"/></param>
        /// <param name="msg_type">消息类型，见成员：<see cref="MsgAckItem.msg_type"/></param>
        /// <param name="client_time">毫秒级时间戳，见成员：<see cref="MsgAckItem.client_time"/></param>
        public LiveMsgItem(string rawData, string msg_id, string msg_type, long client_time)
        {
            data_cached = rawData ?? "";
            this.msg_id = msg_id ?? "";
            this.msg_type = msg_type ?? "";
            this.client_time = client_time;
        }

        /// 令`client_time`使用当前时间戳
        public void UseNowTimestamp()
        {
            client_time = TimeUtil.NowTimestampMs;
        }

        /// 当前Unix毫秒级时间戳 (UTC)
        public static long NowTimestamp() => TimeUtil.NowTimestampMs;
    }

    /// <summary>
    /// 履约Ack请求的互动消息数据，用于履约上报API的请求参数 <see cref="ILiveDYCloudAPI.ReportMsgAck(string, MsgAckItem)"/>,  <see cref="ILiveDYCloudAPI.ReportMsgAck(string, System.Collections.Generic.List{ByteDance.LiveOpenSdk.Legacy.MsgAckItem})"/>
    /// </summary>
    [Serializable]
    [DataContract]
    public class MsgAckItem : LiveMsgItem
    {
        /// 一条消息数据，使用当前时间戳
        public MsgAckItem(string msg_id, string msg_type)
            : base("", msg_id, msg_type)
        {
        }

        /// 一条消息数据，使用指定时间戳
        public MsgAckItem(string msg_id, string msg_type, long _client_time)
            : base("", msg_id, msg_type, _client_time)
        {
        }

        /// 单条消息数据转为List
        public List<MsgAckItem> ToList()
        {
            var list = new List<MsgAckItem> { this };
            return list;
        }

        internal static string ValidateErrorField(MsgAckItem item)
        {
            // 检查item为空、msg_id为空、msg_type为空
            var errorField = string.Empty;
            if (item == null)
                errorField = "`item` is null!";
            else if (string.IsNullOrEmpty(item.msg_id))
                errorField = "`msg_id` is empty!";
            else if (string.IsNullOrEmpty(item.msg_type))
                errorField = "`msg_type` is empty!";
            else if (item.client_time == 0)
                errorField = "`client_time` is 0!";
            return errorField;
        }
    }

    /// <summary>
    /// Connect OnError连接错误时的详细错误数据结构
    /// </summary>
    [Serializable]
    [DataContract]
    public struct ConnectErrorData
    {
        /// 连接错误时，抖音云返回的`code`字段.
        /// <remarks>抖音云目前抛过来的可能是int也可能是string，目前这里适配成统一转为string</remarks>
        [DataMember] public string code;

        /// 连接错误时，抖音云返回的`message`字段
        [DataMember] public string message;

        /// 是否系统会自动重连。 如果`true`，表示本次错误系统会自动重连，开发者上层不需要重新发起连接。 如果`false`，表示连接失败，开发者上层需要自己选择处理，可以稍后重试建连、或先弹框提示再点击后重试建连。
        [DataMember] public bool? willReconnect;
    }
}