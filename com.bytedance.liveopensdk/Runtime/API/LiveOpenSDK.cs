using Douyin.LiveOpenSDK.Modules;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 直播小玩法开放SDK，访问API的入口
    /// </summary>
    public static class LiveOpenSDK
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public static string Version => SdkVersion.Version;

        /// <summary>
        /// 云启动云游戏API
        /// </summary>
        public static ILiveCloudGameAPI CloudGameAPI => s_cloudGameApi ?? CreateCloudGameApi();


        #region internal

        internal const string TAG = nameof(LiveOpenSDK);
        private static readonly SdkCore Core;
        internal static readonly SdkEnv Env;
        internal static readonly SdkDebugInfo DebugInfo;
        internal static readonly SdkDebugLogger Debug;
        private static ILiveCloudGameAPI s_cloudGameApi;

        static LiveOpenSDK()
        {
            Core = new SdkCore();
            Env = Core.Env;
            DebugInfo = Core.SdkDebugInfo;
            Debug = Core.Debug;
            DebugInfo.LogDebugVer();
            DebugInfo.LogDebugEnvs();
            DebugInfo.LogDebugCmdArgs();
            DebugInfo.LogDebugDid();
            var env = Core.Env;
            env.TryInitCloudGameScreen();
#if DEBUG
            SelfTestCode();
#endif
        }

        [RuntimeInitializeOnLoadMethod]
        private static void TryInitCloudGame()
        {
            var env = Core.Env;
            var ret = env.TryInitCloudGameScreen();
            Debug.LogDebug($"TryInitCloudGame screen: {ret}");
        }

        private static ILiveCloudGameAPI CreateCloudGameApi()
        {
            s_cloudGameApi = new ApiCloudGame(Core);
            return s_cloudGameApi;
        }

#if DEBUG
        private static void SelfTestCode()
        {
        }
#endif

        #endregion
    }
}