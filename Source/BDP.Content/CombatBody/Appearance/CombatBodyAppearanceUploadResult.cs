namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 上传当前服装为个人模板的结构化结果。
    /// </summary>
    public sealed class CombatBodyAppearanceUploadResult
    {
        /// <summary>
        /// 上传是否已经完成并提交到账本。
        /// </summary>
        public bool Succeeded;

        /// <summary>
        /// 上传成功后新建的模板；失败时为空。
        /// </summary>
        public CombatBodyAppearanceTemplate Template;

        /// <summary>
        /// 上传失败时对应的语言键；成功时为空。
        /// </summary>
        public string FailureMessageKey;

        /// <summary>
        /// Pawn 参数缺失时的语言键。
        /// </summary>
        public const string MissingPawnMessageKey =
            "BDP_Message_CombatBodyAppearance_MissingPawn";

        /// <summary>
        /// 当前 Pawn 没有可用服装栏或库存时的语言键。
        /// </summary>
        public const string MissingContainerMessageKey =
            "BDP_Message_CombatBodyAppearance_MissingContainer";

        /// <summary>
        /// 无法读取服装外观时的语言键。
        /// </summary>
        public const string CaptureFailedMessageKey =
            "BDP_Message_CombatBodyAppearance_CaptureFailed";

        /// <summary>
        /// 服装转入库存失败时的语言键。
        /// </summary>
        public const string TransferFailedMessageKey =
            "BDP_Message_CombatBodyAppearance_TransferFailed";

        /// <summary>
        /// 当前存档账本不可用时的语言键。
        /// </summary>
        public const string LedgerUnavailableMessageKey =
            "BDP_Message_CombatBodyAppearance_LedgerUnavailable";

        /// <summary>
        /// 模板提交失败时的语言键。
        /// </summary>
        public const string CommitFailedMessageKey =
            "BDP_Message_CombatBodyAppearance_CommitFailed";
    }
}
