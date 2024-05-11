// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ByteDance.LiveOpenSdk.Legacy;

namespace ByteDance.LiveOpenSdk.Contracts
{
    [Serializable]
    [DataContract]
    internal class OpenApiBaseResponse
    {
        [DataMember(Name = "errcode")] public int errCode;
        [DataMember(Name = "errmsg")] public string errMsg = "";
    }

    [Serializable]
    [DataContract]
    internal class WebcastMateInfoResponse : OpenApiBaseResponse
    {
        [Serializable]
        [DataContract]
        internal class Payload
        {
            [DataMember(Name = "info")] public WebcastInfo? info;
            [DataMember(Name = "ack_cfg")] public List<AckConfig>? ackConfig;
        }

        [DataMember(Name = "data")] public Payload? data;
    }
}