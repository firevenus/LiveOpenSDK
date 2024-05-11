namespace Douyin.LiveOpenSDK.Data
{
    [System.Serializable]
    public class RequestErrorResponse
    {
        public int errcode; //调用接口后返回的错误码
        public string errmsg; //调用接口后返回的错误信息
        public string logId; //开平接口返回的logid
    }

    public static class ErrorInfoDefine
    {
        #region internal errors

        public static readonly RequestErrorResponse Success = new RequestErrorResponse ()
        {
            errcode = 0,
            errmsg = "Success"
        };

        /// <summary>
        /// appId为空
        /// </summary>
        public static readonly RequestErrorResponse AppidEmptyErrorCode = new RequestErrorResponse ()
        {
            errcode = 9993,
            errmsg = "Input appid is empty"
        };

        /// <summary>
        /// 已经请求了初始化，请稍等
        /// </summary>
        public static readonly RequestErrorResponse AlreadyRequestInitErrorCode = new RequestErrorResponse ()
        {
            errcode = 9994,
            errmsg = "Already request init, please wait."
        };

        /// <summary>
        /// 初始化超时
        /// </summary>
        public static readonly RequestErrorResponse InitTimeoutErrorCode = new RequestErrorResponse ()
        {
            errcode = 9995,
            errmsg = "Init timeout"
        };

        /// <summary>
        /// SDK已经初始化过了
        /// </summary>
        public static readonly RequestErrorResponse AlreadyInitializeErrorCode = new RequestErrorResponse ()
        {
            errcode = 9996,
            errmsg = "LiveOpenSDK has already init"
        };

        /// <summary>
        /// 需要先开启查询任务
        /// </summary>
        public static readonly RequestErrorResponse NotStartTaskErrorCode = new RequestErrorResponse ()
        {
            errcode = 9997,
            errmsg = "Need start task first"
        };
        /// <summary>
        /// SDK需要先初始化或网络重连中
        /// </summary>
        public static readonly RequestErrorResponse NotInitializeErrorCode = new RequestErrorResponse ()
        {
            errcode = 9998,
            errmsg = "LiveOpenSDK need init or net is reconnecting"
        };

        /// <summary>
        /// 网络断联
        /// </summary>
        public static readonly RequestErrorResponse NetDisconnectedErrorCode = new RequestErrorResponse ()
        {
            errcode = 9999,
            errmsg = "Net disconnected, callback return error!"
        };



        #endregion
    }


}