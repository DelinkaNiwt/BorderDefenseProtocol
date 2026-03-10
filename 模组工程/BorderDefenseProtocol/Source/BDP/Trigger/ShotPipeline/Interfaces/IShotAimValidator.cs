namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 可选接口：参与Targeting子步骤的目标验证
    /// 无效时阻止射击并显示原因
    /// </summary>
    public interface IShotAimValidator
    {
        AimValidation ValidateTarget(ShotSession session, Verse.LocalTargetInfo target);
    }
}
