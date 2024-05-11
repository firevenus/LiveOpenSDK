// Copyright@www.bytedance.com
// Author: liziang
// Date: 2024/02/19
// Description:

using System;
using System.Runtime.Serialization;

namespace Douyin.LiveOpenSDK.Data
{
    [Serializable]
    [DataContract]
    public class DummyMessage
    {
        [DataMember(Name = "msg_id")] public string msg_id;
        [DataMember(Name = "nick-name")] public string nick_name;
    }

    [Serializable]
    [DataContract]
    public class DYCloudSocketMessage
    {
        [DataMember(Name = "msg_id")] public string msg_id;
        [DataMember(Name = "msg_type")] public string msg_type;

        /// 透传, 预留给开发者
        [DataMember(Name = "extra_data")] public string extra_data;

        /// 透传
        [DataMember(Name = "body")] public string body;

        [DataMember(Name = "room_id")] public string room_id;

        /// 是否时测试数据
        [DataMember(Name = "test")] public bool test;
    }
}