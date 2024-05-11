// Copyright (c) Bytedance. All rights reserved.
// Description:

using Newtonsoft.Json;

namespace ByteDance.LiveOpenSdk.Utilities
{
    internal static class JsonUtils
    {
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}