// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Runtime.Serialization;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine.Networking;

namespace Douyin.LiveOpenSDK.Data
{
    [System.Serializable]
    [DataContract]
    internal class HttpAPIReq
    {
        /// 转Json字符串
        public virtual string ToJsonString() => SdkUtils.ToJsonString(this);

        /// 转Json字符串
        public override string ToString() => SdkUtils.ToJsonString(this);
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
            ListJsonString = SdkUtils.ToJsonString(List);
            return ListJsonString;
        }
    }

    [System.Serializable]
    [DataContract]
    internal class AckResp : APIResponse
    {
        /// 错误码，非0为失败
        [DataMember] public int err_no;

        [DataMember] public string err_msg;

        [DataMember] public string logid;
    }

    internal class WebcastInfoResponse : HttpAPIResponse
    {
        internal WebcastInfoData Data { get; set; }

        internal override void AcceptWebRequest(UnityWebRequest req)
        {
            base.AcceptWebRequest(req);
            var rawData = this.body;
            if (base.IsResultSuccess && string.IsNullOrEmpty(rawData))
            {
                code = APICode.APIResponseError;
                errorMsg = "body is empty!";
                return;
            }

            try
            {
                ParseResponseData(rawData);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                AcceptException(e);
            }
        }

        // note: need to try catch for parse
        internal void ParseResponseData(string rawData)
        {
            respSource = RespSourceType.SDKClient;
            var data = SdkUtils.FromJsonString<WebcastInfoData>(rawData);
            Data = data;
            if (data == null)
                throw new System.Data.DataException("data is null");
            if (data.data == null)
                throw new System.Data.DataException("data.data is null");

            var info = data.data.info;
            var ack_cfg = data.data.ack_cfg;
            if (info == null)
                throw new System.Data.DataException("info is null");
            if (ack_cfg == null)
                throw new System.Data.DataException("ack_cfg is null");
            if (info.room_id == 0)
                throw new System.Data.DataException("room_id is 0");
            if (info.anchor_open_id == null)
                throw new System.Data.DataException("anchor_open_id is null");
        }

        internal InitWebcastInfoResponse ToInitWebcastInfoResponse()
        {
            var webcastInfo = this.Data?.data?.info;
            var initInfoResp = new InitWebcastInfoResponse
            {
                code = this.code,
                errorMsg = this.errorMsg,
                respSource = this.respSource,
                statusCode = this.statusCode,
                reqResultType = this.reqResultType,
                room_id = webcastInfo?.room_id.ToString() ?? "",
                anchor_open_id = webcastInfo?.anchor_open_id ?? "",
                avatar_url = webcastInfo?.avatar_url ?? "",
                nick_name = webcastInfo?.nick_name ?? "",
            };
            return initInfoResp;
        }

        internal static bool SelfTest()
        {
            {
                var json =
                    "{\"room_id\":1111122222333334444,\"anchor_open_id\":\"xxx111\",\"avatar_url\":\"xxx222\",\"nick_name\":\"\ud83d\ude48df%三角形是孤独的三角形\"}";
                var info = SdkUtils.FromJsonString<WebcastInfo>(json);
                AssertUtil.IsTrue(info.room_id == 1111122222333334444);
                AssertUtil.IsTrue(info.anchor_open_id == "xxx111");
                AssertUtil.IsTrue(info.avatar_url == "xxx222");
                AssertUtil.IsFalse(string.IsNullOrEmpty(info.nick_name));
            }
            {
                var json =
                    "{\"data\":{\"info\":{\"room_id\":1111122222333334444,\"anchor_open_id\":\"xxx111\",\"avatar_url\":\"xxx222\",\"nick_name\":\"🙈df%三角形是孤独的三角形\"},\"ack_cfg\":[{\"msg_type\":\"live_gift\",\"ack_type\":1,\"batch_interval\":3,\"batch_max_num\":5},{\"msg_type\":\"live_gift\",\"ack_type\":2,\"batch_interval\":3,\"batch_max_num\":5}]}}";
                var data = SdkUtils.FromJsonString<WebcastInfoData>(json);
                AssertUtil.IsTrue(data.data.info.room_id == 1111122222333334444);

                var resp = new WebcastInfoResponse();
                resp.ParseResponseData(json);
                AssertUtil.IsTrue(resp.Data.data.info.room_id == 1111122222333334444);
            }
            return true;
        }
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