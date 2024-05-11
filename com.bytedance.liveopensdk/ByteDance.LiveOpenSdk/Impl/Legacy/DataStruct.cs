// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Runtime.Serialization;
using ByteDance.LiveOpenSdk.Utilities;

namespace ByteDance.LiveOpenSdk.Legacy
{
    [System.Serializable]
    [DataContract]
    internal class HttpAPIReq
    {
        /// 转Json字符串
        public virtual string ToJsonString() => JsonUtils.Serialize(this);

        /// 转Json字符串
        public override string ToString() => JsonUtils.Serialize(this);
    }

    [System.Serializable]
    [DataContract]
    internal class AckAPIReq : HttpAPIReq
    {
        [DataMember] public string room_id;
        [DataMember] public string app_id;
        [DataMember] public int ack_type; // 上报类型，1：原始指令到达后上报，2：渲染后上报

        /** 上报数据，json字符串
         [{
           "msg_id": "xxxx", // 唯一标识，平台推送payload数据里的msg_id
           "msg_type": "xxx", // 消息类型，live_gift：礼物消息，live_comment：评论消息
           "client_time": 1705989099973 // 毫秒级时间戳，当ack_type为1时即为指令收到后的时间，当ack_type为2时即为渲染成功后的时间
         }]
        */
        [DataMember] public string data;
    }

    internal class AckReqMsgListConverter
    {
        public string ListJsonString { get; set; }
        public List<MsgAckItem> List { get; }

        public AckReqMsgListConverter(List<MsgAckItem> list)
        {
            List = list ?? new List<MsgAckItem>();
        }

        public string ToJsonString()
        {
            ListJsonString = JsonUtils.Serialize(List);
            return ListJsonString;
        }
    }

    [System.Serializable]
    [DataContract]
    internal class AckResp
    {
        /// 错误码，非0为失败
        [DataMember] public int err_no;

        [DataMember] public string err_msg;

        [DataMember] public string logid;
    }

    [System.Serializable]
    [DataContract]
    internal class WebcastInfoData
    {
        [System.Serializable]
        [DataContract]
        internal class Data
        {
            [DataMember] public WebcastInfo info;
            [DataMember] public List<AckConfig> ack_cfg; // ack上报配置
        }

        [DataMember] public Data data;
    }

    /// 下发的直播信息 WebcastInfo
    [System.Serializable]
    [DataContract]
    internal class WebcastInfo
    {
        [DataMember] public long room_id; // 房间ID int64
        [DataMember] public string anchor_open_id; // 主播openID
        [DataMember] public string avatar_url; // 主播头像地址
        [DataMember] public string nick_name; // 主播昵称
    }

    /// 下发的履约配置 AckConfig
    [System.Serializable]
    [DataContract]
    internal class AckConfig
    {
        [DataMember] public string msg_type; //消息类型

        [DataMember] public long ack_type; // 上报类型，1：指令到达后上报，2：渲染成功后上报

        // note: 两个batch的发送逻辑：`或`的关系，即消息缓存有未发送等待达到等到interval、要发，或，消息数量达到了max_num就发
        [DataMember] public long batch_interval; //批次间隔，秒
        [DataMember] public long batch_max_num; //批次最大条目
    }
}