// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace ByteDance.LiveOpenSdk.Legacy
{
    /// <summary>
    /// 直播消息类型
    /// </summary>
    public class LiveMsgType
    {
        /// 1. 评论：live_comment (弹幕)
        public const string live_comment = "live_comment";

        /// 2. 礼物：live_gift
        public const string live_gift = "live_gift";

        /// 3. 点赞：live_like
        public const string live_like = "live_like";

        public static bool IsValidType(string msg_type)
        {
            return msg_type switch
            {
                live_comment => true,
                live_gift => true,
                live_like => true,
                _ => false
            };
        }

        /// <summary>
        /// 如果无效值，返回`string.Empty`
        /// </summary>
        public static string FromInt(int msg_type_int)
        {
            return msg_type_int switch
            {
                1 => live_comment,
                2 => live_gift,
                3 => live_like,
                _ => string.Empty
            };
        }

        public static int[] GetInts()
        {
            return new[] {1, 2, 3};
        }

        internal static int ToInt(string msg_type)
        {
            return msg_type switch
            {
                live_comment => 1,
                live_gift => 2,
                live_like => 3,
                _ => -1
            };
        }

        public static string ToChineseText(string msg_type)
        {
            return msg_type switch
            {
                live_comment => "评论",
                live_gift => "礼物",
                live_like => "点赞",
                _ => msg_type
            };
        }
    }
}