namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>
    /// 可持久识别、可本地化展示的组合失败原因。
    /// </summary>
    public sealed class ChipCombinationFailureReason
    {
        /// <summary>供测试、迁移与日志使用的稳定代码。</summary>
        public string Code { get; set; }

        /// <summary>玩家可见文本的语言包键。</summary>
        public string TranslationKey { get; set; }

        /// <summary>创建一条失败原因。</summary>
        public ChipCombinationFailureReason(string code, string translationKey)
        {
            Code = code;
            TranslationKey = translationKey;
        }
    }
}
