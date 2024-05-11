// Copyright (c) Bytedance. All rights reserved.
// Description:

using dyCloudUnitySDK;

namespace Douyin.LiveOpenSDK.Data
{
    [System.Serializable]
    public class SdkException : System.Exception
    {
        public int code;
        public string errorMsg;
        public string api;

        public RespSourceType respSource;

        internal SdkException(string message) : base(message)
        {
        }

        public virtual string ToErrorLog() => $"code: {code} source: {respSource} errorMsg: {errorMsg}";
    }

    [System.Serializable]
    public class DYCloudSdkException : SdkException
    {
        public string logid;

        internal DYCloudSdkException(CustomExpection e) : base(e.Message)
        {
            AcceptException(e);
        }

        internal DYCloudSdkException AcceptException(CustomExpection e)
        {
            respSource = RespSourceType.DYCloudSDK;
            if (int.TryParse(e.code, out int exCode))
            {
                code = exCode;
            }
            else
            {
                code = APICode.APIResponseError;
            }

            // Exception是异常，避免解析出 code 0
            if (code == 0)
            {
                code = APICode.APIResponseError;
            }

            api = e.api;
            errorMsg = e.Message;
            logid = e.logid;
            return this;
        }

        public override string ToErrorLog() => $"code: {code} source: {respSource} errorMsg: {errorMsg} logid: {logid}";

    }
}