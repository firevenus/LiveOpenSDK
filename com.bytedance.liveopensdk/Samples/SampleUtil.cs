// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Samples
{
    public class SampleUtil
    {
        public static string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static string ToJsonString(object obj, bool indent)
        {
            return JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None);
        }

        public static T FromJsonString<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return default;
            }
        }
    }
}