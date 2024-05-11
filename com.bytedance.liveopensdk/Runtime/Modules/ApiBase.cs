// Copyright (c) Bytedance. All rights reserved.
// Description:

using Douyin.LiveOpenSDK.Utilities;

namespace Douyin.LiveOpenSDK.Modules
{
    internal abstract class ApiBase
    {
        private readonly string _tag;
        protected readonly SdkCore Core;
        protected readonly SdkDebugLogger Debug;
        protected static SdkDebugLogger s_Debug => LiveOpenSDK.Debug;
        protected readonly AppQuitHelper QuitHelper;
        protected MonoManager MonoManager;
        protected SdkTeaReporter Reporter => SdkTeaReporter.Instance;

        internal ApiBase(SdkCore core, string tag)
        {
            Core = core;
            _tag = tag;
            Debug = core.Debug;
            QuitHelper = new AppQuitHelper(tag);
            CheckInitMonoManager();
        }

        ~ApiBase()
        {
            UnityEngine.Debug.Log($"~ApiBase released tag: {_tag}");
        }

        protected void CheckInitMonoManager()
        {
            if (MonoManager == null)
                MonoManager = MonoManager.GetInstance();
        }
    }
}