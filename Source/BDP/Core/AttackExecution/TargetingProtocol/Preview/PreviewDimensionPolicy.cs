namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// Preview 维度策略。
    /// 它只负责提供各预览维度的默认值与读取方式，不直接承担绘制逻辑。
    /// </summary>
    internal static class PreviewDimensionPolicy
    {
        /// <summary>
        /// 用原版基线初始化一份 Preview 记录。
        /// 无模块时所有维度都保持沿用原版。
        /// </summary>
        public static void ApplyBaseline(PreviewRecord record)
        {
            if (record == null)
            {
                return;
            }

            record.UseVanillaRangeRing = true;
            record.UseVanillaTargetHighlight = true;
            record.UseVanillaFieldRadius = true;
            record.UseVanillaMouseAttachment = true;
        }

        /// <summary>
        /// 读取某个预览维度当前是否继续沿用原版。
        /// </summary>
        public static bool UsesVanilla(PreviewRecord record, PreviewDimension dimension)
        {
            if (record == null)
            {
                return false;
            }

            switch (dimension)
            {
                case PreviewDimension.RangeRing:
                    return record.UseVanillaRangeRing;
                case PreviewDimension.TargetHighlight:
                    return record.UseVanillaTargetHighlight;
                case PreviewDimension.FieldRadius:
                    return record.UseVanillaFieldRadius;
                case PreviewDimension.MouseAttachment:
                    return record.UseVanillaMouseAttachment;
                default:
                    return false;
            }
        }
    }
}
