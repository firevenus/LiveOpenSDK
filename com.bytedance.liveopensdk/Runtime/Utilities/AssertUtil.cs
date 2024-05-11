// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace Douyin.LiveOpenSDK.Utilities
{
    public static class AssertUtil
    {
        public static void IsTrue(bool condition, string errorMessage = null)
        {
            if (!condition)
            {
                throw new System.Exception(errorMessage ?? "AssertUtil.IsTrue failed.");
            }
        }

        public static void IsFalse(bool condition, string errorMessage = null)
        {
            if (condition)
            {
                throw new System.Exception(errorMessage ?? "AssertUtil.IsFalse failed.");
            }
        }

        public static void AreEqual(object expected, object actual, string errorMessage = null)
        {
            // ReSharper disable once RedundantNameQualifier
            if (!object.Equals(expected, actual))
            {
                throw new System.Exception(errorMessage ?? $"AssertUtil.AreEqual failed. Expected: {expected}, Actual: {actual}.");
            }
        }
    }
}