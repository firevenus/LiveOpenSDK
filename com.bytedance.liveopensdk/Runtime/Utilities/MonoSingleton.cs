// Copyright (c) Bytedance. All rights reserved.
// Description:

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Douyin.LiveOpenSDK.Utilities
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        protected static T _Instance;

        private bool _destroyedInAwake = false;
        protected SdkDebugLogger Logger { get; } = new SdkDebugLogger(TName);

        protected bool IsCompiling
        {
#if UNITY_EDITOR
            get { return EditorApplication.isCompiling; }
#else
            get { return false; }
#endif
        }

        private static string TName => typeof(T).Name;

        public virtual string InstanceName => typeof(T).Name;

        protected virtual bool IsThisInstance => _Instance == this;

        public static bool HasInstance()
        {
            return _Instance != null;
        }

        public static T GetInstance()
        {
            if (_Instance == null)
            {
                _Instance = FindOrCreateInstance();
            }

            return _Instance;
        }

        private static T FindOrCreateInstance()
        {
            var instance = FindInstance();
            if (instance == null)
                instance = CreateInstance();
            return instance;
        }

        private static T FindInstance()
        {
            return FindObjectOfType<T>();
        }

        private static T CreateInstance()
        {
#if UNITY_EDITOR
            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning($"CreateInstance {TName} - unexpected isCompiling!");
            }

            if (Application.isBatchMode)
            {
                Debug.Log($"CreateInstance {typeof(T).Name} - isBatchMode");
            }
#endif
            var go = new GameObject();
            if (Application.isPlaying)
                go.hideFlags = HideFlags.DontSave;
            else
                go.hideFlags = HideFlags.HideAndDontSave;
            var instance = go.AddComponent<T>();
            go.name = instance.InstanceName;
            return instance;
        }

        internal static void DestroyInstance()
        {
            if (HasInstance())
            {
                var go = _Instance.gameObject;
                if (go != null)
                    DestroyImmediate(go);
                _Instance = null;
            }
        }

        protected virtual void Awake()
        {
            if (_Instance != null)
            {
                if (_Instance != this)
                {
                    _destroyedInAwake = true;
                    DestroyImmediate(gameObject);
                }

                return;
            }

            _Instance = this as T;

            if (_Instance != null)
                DontDestroyOnLoad(_Instance.gameObject);
        }

        protected virtual void OnEnable()
        {
            if (!IsThisInstance)
            {
                Logger.LogWarning("unexpected OnEnable. Instance mismatch.");
            }
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
#endif
        }

        protected virtual void OnDisable()
        {
            if (_destroyedInAwake)
                return;
            if (!IsThisInstance)
            {
                Logger.LogWarning("unexpected OnDisable. Instance mismatch.");
            }
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.ExitingPlayMode) return;
            if (!HasInstance()) return;
            DestroyInstance();
        }

        private void OnApplicationQuit()
        {
            DestroyImmediate(this.gameObject);
        }
#endif
    }
}