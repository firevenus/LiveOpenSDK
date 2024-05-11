using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Douyin.LiveOpenSDK.Plugins.LitJson;
using UnityEngine;
using UnityEngine.Networking;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal interface ITeaDataProvider
    {
        string TestDeviceId { get; }

        Dictionary<string, object> CustomValues { get; }
    }

    internal class TestTeamDataProvider : ITeaDataProvider
    {
        public TestTeamDataProvider(string testDeviceId)
        {
            TestDeviceId = testDeviceId;
        }

        public string TestDeviceId { get; }
        public Dictionary<string, object> CustomValues { get; } = new Dictionary<string, object>();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class TeaSdk
    {
        private const string CN = "https://mcs.zijieapi.com/v1/list";
        private const string CN_TEST = "https://mcs.zijieapi.com/v1/list_test";

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private struct TeaEvent
        {
            public string @event;
            public double local_time_ss;
            public string @params;
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class Header
        {
            public string device_id;
            public string app_version;
            public string app_channel;
            public string os_version;
            public string os_name;
            public int app_id;
            public Dictionary<string, object> custom;
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class User
        {
            public string user_unique_id;
        }

        private Header _header;
        private User _user;
        private Dictionary<string, object> _custom;

        public void Init(int appId, string uuid, string appChannel, string appVersion)
        {
            _header = new Header
            {
                app_channel = appChannel,
                app_version = GetAppVersion(appVersion),
                os_name = GetOSName(),
                os_version = Environment.OSVersion.VersionString,
                app_id = appId,
                device_id = uuid,
            };

            _user = new User
            {
                user_unique_id = uuid
            };
        }

        private string GetAppVersion(string appVersion)
        {
            return appVersion; // 1.0.0正确，1.0会为unknown
        }

        private string GetOSName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXEditor:
                    return "Mac";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
            }
            return "unknown";
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private class CollectData
        {
            public TeaEvent[] events;
            public Header header;
            public User user;
        }

        public async void Collect(string eventName, string eventParams, ITeaDataProvider provider)
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var custom = new Dictionary<string, object>();
            if (provider?.CustomValues != null)
            {
                foreach (var pair in provider.CustomValues)
                {
                    custom.Add(pair.Key, pair.Value);
                }
            }

            if (_custom != null)
            {
                foreach (var pair in _custom)
                {
                    custom.Add(pair.Key, pair.Value);
                }
            }

            _header.custom = custom;
            var user = _user;
            user.user_unique_id = _user.user_unique_id;
            //user.user_unique_id = provider?.TestDeviceId ?? _user.user_unique_id;
            var data = new CollectData
            {
                events = new[]
                {
                    new TeaEvent
                    {
                        @event = eventName,
                        local_time_ss = ts.TotalMilliseconds,
                        @params = eventParams
                    }
                },
                header = _header,
                user = user
            };
            var json = JsonMapper.ToJson(data);
            var arrJson = $"[{json}]";
#if DEVELOPMENT_BUILD
            string url = CN_TEST;
#else
            string url = CN;
#endif
            var request = new UnityWebRequest(url, "POST");
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(arrJson));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest();
            while (!request.isDone)
            {
                await Task.Yield();
            }
        }

        public void AddCustom(string name, object value)
        {
            if (_custom == null)
            {
                _custom = new Dictionary<string, object>();
                _header.custom = _custom;
            }
            _custom?.Add(name, value);
        }
    }
}