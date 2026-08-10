namespace BDP.Core.AttackExecution.RangedProtocol.ProjectileInit
{
    /// <summary>
    /// ProjectileInit 阶段模块接口。
    /// 模块只提交初始化计划贡献，不直接接管 projectile 宿主。
    /// </summary>
    public interface IProjectileInitStageModule
    {
        void Contribute(in ProjectileInitStageContext context, ProjectileInitContribution contribution);
    }
}
