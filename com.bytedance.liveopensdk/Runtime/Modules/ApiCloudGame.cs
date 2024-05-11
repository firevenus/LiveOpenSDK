// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace Douyin.LiveOpenSDK.Modules
{
    internal class ApiCloudGame : ApiBase, ILiveCloudGameAPI
    {
        // ReSharper disable once UnusedMember.Local
        private const string TAG = "CloudGameAPI";

        public ApiCloudGame(SdkCore core) : base(core, TAG)
        {
            LogDebugInfo();
#if DEBUG
            SelfTestCode();
#endif
        }

#if DEBUG
        private void SelfTestCode()
        {
        }
#endif

        private void LogDebugInfo()
        {
            var msg = $"{TAG} - IsCloudGame: {IsCloudGame()}" +
                      $", IsStartFromMobile: {IsStartFromMobile()}";
            Debug.LogDebug(msg);
        }

        public bool IsCloudGame()
        {
            return Core.Env.IsStartFromCloud();
        }

        public bool IsStartFromMobile()
        {
            return Core.Env.IsStartFromMobile();
        }

        public void TryInitFullScreen()
        {
            Core.Env.TryInitCloudGameScreen();
        }
    }
}