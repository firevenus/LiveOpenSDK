// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Runtime.Serialization;

namespace ByteDance.LiveOpenSdk.DyCloud
{
    /// <summary>
    /// 抖音云下行消息的结构。
    /// </summary>
    [DataContract]
    public class DyCloudSocketMessage
    {
        /// <summary>
        /// 消息 ID。
        /// </summary>
        [DataMember(Name = "msg_id")] public string MsgId = "";

        /// <summary>
        /// 消息类型，例如 live_like/live_comment/live_gift
        /// </summary>
        [DataMember(Name = "msg_type")] public string MsgType = "";

        /// <summary>
        /// 消息内容，由开发者的服务端指定。
        /// </summary>
        [DataMember(Name = "data")] public string Data = "";

        /// <summary>
        /// 消息额外内容，由开发者的服务端指定。
        /// </summary>
        [DataMember(Name = "extra_data")] public string ExtraData = "";
    }
}