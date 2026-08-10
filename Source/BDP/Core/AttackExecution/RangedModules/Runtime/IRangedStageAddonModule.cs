namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程阶段附加挂件接口。
    /// 它只在阶段主结果确定之后执行附加逻辑，不参与主逻辑裁决。
    /// </summary>
    public interface IRangedStageAddonModule
    {
        /// <summary>
        /// 在指定阶段主结果已经确定之后执行附加逻辑。
        /// </summary>
        void AfterStage(in RangedStageAddonContext context);
    }
}
