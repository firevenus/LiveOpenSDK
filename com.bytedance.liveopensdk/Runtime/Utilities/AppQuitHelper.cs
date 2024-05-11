// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal class AppQuitHelper
    {
        public AppQuitHelper(string tag)
        {
            Tag = tag;
            CheckInitQuitHandler();
        }

        private readonly string Tag;
        private bool _hasInitQuitHandler;
        private bool _hasAppQuit;
        internal event Action onQuitAction;

        public bool IsAppQuitting()
        {
            CheckInitQuitHandler();
            return _hasAppQuit;
        }

        protected void CheckInitQuitHandler()
        {
            if (_hasAppQuit)
                return;
            if (_hasInitQuitHandler)
                return;
            try
            {
                _hasInitQuitHandler = true;
                UnityEngine.Application.quitting += OnAppQuitting;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log($"{Tag} AppQuitHelper exception: {e}");
                _hasInitQuitHandler = false;
            }
        }

        protected void OnAppQuitting()
        {
            _hasAppQuit = true;
            UnityEngine.Application.quitting -= OnAppQuitting;
            UnityEngine.Debug.Log($"{Tag} AppQuitHelper OnGameQuitting");
            // invoke to observers first. clear after invoke to observers as we are quiting
            onQuitAction?.Invoke();
            onQuitAction = null;
        }
    }
}