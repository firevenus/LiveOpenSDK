// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace Douyin.LiveOpenSDK.Data
{
    internal static class ConstsInternal
    {
        internal const string LaunchCmdArg_Token = "-token=";
        internal const string ApiHost_Webcast = "webcast.bytedance.com";
        internal const string ApiPath_Ack = "/api/live_data/ack";
        internal const string ApiPath_WebcastInfo = "/api/webcastmate/info";

        internal static class DYCloud
        {
            internal const string TestCallPath = "/hello?name=xxx";
            internal const string WSErrorIgnoreCode = "4998";
        }

        internal static class FeatureGates
        {
            internal const string _RequestWebcastInfo = "_RequestWebcastInfo";
            internal const string _RequestAck = "_RequestAck";
        }
    }
}