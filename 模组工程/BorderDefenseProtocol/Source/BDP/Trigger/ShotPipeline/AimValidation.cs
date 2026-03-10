namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 目标验证结果：IShotAimValidator产出
    /// </summary>
    public struct AimValidation
    {
        public bool IsValid;
        public string InvalidReason;  // 修改：统一命名

        public static AimValidation Valid => new AimValidation { IsValid = true };

        public static AimValidation Invalid(string reason)
        {
            return new AimValidation { IsValid = false, InvalidReason = reason };
        }
    }
}
