using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.Visual
{
    /// <summary>
    /// 投射物视觉附加提供器接口。
    /// 它可挂在 `projectile ThingDef（投射物定义）` 或发射来源 `ThingDef.modExtensions（定义扩展）` 上。
    /// 主模组只把它当成中性视觉工厂，用于为每一发投射物创建自己的视觉附加件。
    /// </summary>
    public interface IProjectileVisualAttachmentProvider
    {
        /// <summary>
        /// 为当前投射物创建一份新的视觉附加件实例。
        /// </summary>
        /// <returns>新的视觉附加件；返回空表示当前提供器不参与本发投射物。</returns>
        IProjectileVisualAttachment CreateAttachment(
            ProjectileVisualAppearanceOverrides visualAppearanceOverrides);
    }

    /// <summary>
    /// 投射物视觉附加件接口。
    /// 主模组只向它广播中性的视觉事实，不向它暴露内部飞行业务对象。
    /// </summary>
    public interface IProjectileVisualAttachment
    {
        /// <summary>
        /// 接收投射物发射事件。
        /// </summary>
        /// <param name="context">本次发射的中性视觉上下文。</param>
        void OnLaunch(in ProjectileVisualLaunchContext context);

        /// <summary>
        /// 接收一次真实飞行样本。
        /// 样本只表示“本次推进前后的位置变化”，不包含任何业务拆段结论。
        /// </summary>
        /// <param name="context">本次飞行样本的中性视觉上下文。</param>
        void OnFlightSample(in ProjectileVisualFlightSampleContext context);

        /// <summary>
        /// 接收存读档恢复事件。
        /// </summary>
        /// <param name="context">当前读档恢复点的中性视觉上下文。</param>
        void OnRestored(in ProjectileVisualRestoreContext context);

        /// <summary>
        /// 接收投射物结束事件。
        /// </summary>
        /// <param name="context">当前结束点的中性视觉上下文。</param>
        void OnTerminate(in ProjectileVisualTerminateContext context);
    }

    /// <summary>
    /// 投射物发射时的视觉上下文。
    /// 这里暴露的是可跨程序集使用的中性事实，不泄漏主模组内部对象。
    /// </summary>
    public readonly struct ProjectileVisualLaunchContext
    {
        /// <summary>
        /// 当前投射物所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前投射物定义。
        /// </summary>
        public ThingDef ProjectileDef { get; }

        /// <summary>
        /// 当前投射物实体标识。
        /// </summary>
        public string ProjectileThingId { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前投射物发射起点。
        /// 坐标已归一到共享飞行平面，高度显示由附加件自己决定。
        /// </summary>
        public Vector3 LaunchOrigin { get; }

        /// <summary>
        /// 当前投射物发射方向。
        /// </summary>
        public Vector3 LaunchDirection { get; }

        /// <summary>
        /// 用一组中性发射事实初始化发射上下文。
        /// </summary>
        /// <param name="map">当前地图。</param>
        /// <param name="projectileDef">当前投射物定义。</param>
        /// <param name="projectileThingId">当前投射物实体标识。</param>
        /// <param name="attackInstanceId">当前攻击实例标识。</param>
        /// <param name="resultId">当前正式结果标识。</param>
        /// <param name="launchOrigin">当前投射物发射起点。</param>
        /// <param name="launchDirection">当前投射物发射方向。</param>
        public ProjectileVisualLaunchContext(
            Map map,
            ThingDef projectileDef,
            string projectileThingId,
            string attackInstanceId,
            string resultId,
            Vector3 launchOrigin,
            Vector3 launchDirection)
        {
            Map = map;
            ProjectileDef = projectileDef;
            ProjectileThingId = projectileThingId;
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            LaunchOrigin = launchOrigin;
            LaunchDirection = launchDirection;
        }
    }

    /// <summary>
    /// 投射物飞行样本的视觉上下文。
    /// 这里表示的是“本次推进产生的一段原始样本”，不是业务层最终拆段结果。
    /// </summary>
    public readonly struct ProjectileVisualFlightSampleContext
    {
        /// <summary>
        /// 当前投射物所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前投射物定义。
        /// </summary>
        public ThingDef ProjectileDef { get; }

        /// <summary>
        /// 当前投射物实体标识。
        /// </summary>
        public string ProjectileThingId { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前样本起点。
        /// 坐标已归一到共享飞行平面。
        /// </summary>
        public Vector3 SampleStart { get; }

        /// <summary>
        /// 当前样本终点。
        /// 坐标已归一到共享飞行平面。
        /// </summary>
        public Vector3 SampleEnd { get; }

        /// <summary>
        /// 本次样本对应的推进刻数。
        /// </summary>
        public int TickDelta { get; }

        /// <summary>
        /// 用一组中性飞行样本事实初始化飞行样本上下文。
        /// </summary>
        /// <param name="map">当前地图。</param>
        /// <param name="projectileDef">当前投射物定义。</param>
        /// <param name="projectileThingId">当前投射物实体标识。</param>
        /// <param name="attackInstanceId">当前攻击实例标识。</param>
        /// <param name="resultId">当前正式结果标识。</param>
        /// <param name="sampleStart">当前样本起点。</param>
        /// <param name="sampleEnd">当前样本终点。</param>
        /// <param name="tickDelta">本次样本对应的推进刻数。</param>
        public ProjectileVisualFlightSampleContext(
            Map map,
            ThingDef projectileDef,
            string projectileThingId,
            string attackInstanceId,
            string resultId,
            Vector3 sampleStart,
            Vector3 sampleEnd,
            int tickDelta)
        {
            Map = map;
            ProjectileDef = projectileDef;
            ProjectileThingId = projectileThingId;
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            SampleStart = sampleStart;
            SampleEnd = sampleEnd;
            TickDelta = tickDelta;
        }
    }

    /// <summary>
    /// 投射物读档恢复时的视觉上下文。
    /// </summary>
    public readonly struct ProjectileVisualRestoreContext
    {
        /// <summary>
        /// 当前投射物所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前投射物定义。
        /// </summary>
        public ThingDef ProjectileDef { get; }

        /// <summary>
        /// 当前投射物实体标识。
        /// </summary>
        public string ProjectileThingId { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前恢复位置。
        /// 坐标已归一到共享飞行平面。
        /// </summary>
        public Vector3 CurrentPosition { get; }

        /// <summary>
        /// 用一组中性恢复事实初始化恢复上下文。
        /// </summary>
        /// <param name="map">当前地图。</param>
        /// <param name="projectileDef">当前投射物定义。</param>
        /// <param name="projectileThingId">当前投射物实体标识。</param>
        /// <param name="attackInstanceId">当前攻击实例标识。</param>
        /// <param name="resultId">当前正式结果标识。</param>
        /// <param name="currentPosition">当前恢复位置。</param>
        public ProjectileVisualRestoreContext(
            Map map,
            ThingDef projectileDef,
            string projectileThingId,
            string attackInstanceId,
            string resultId,
            Vector3 currentPosition)
        {
            Map = map;
            ProjectileDef = projectileDef;
            ProjectileThingId = projectileThingId;
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            CurrentPosition = currentPosition;
        }
    }

    /// <summary>
    /// 投射物结束时的视觉上下文。
    /// </summary>
    public readonly struct ProjectileVisualTerminateContext
    {
        /// <summary>
        /// 当前投射物所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前投射物定义。
        /// </summary>
        public ThingDef ProjectileDef { get; }

        /// <summary>
        /// 当前投射物实体标识。
        /// </summary>
        public string ProjectileThingId { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前结束位置。
        /// 坐标已归一到共享飞行平面。
        /// </summary>
        public Vector3 CurrentPosition { get; }

        /// <summary>
        /// 用一组中性结束事实初始化结束上下文。
        /// </summary>
        /// <param name="map">当前地图。</param>
        /// <param name="projectileDef">当前投射物定义。</param>
        /// <param name="projectileThingId">当前投射物实体标识。</param>
        /// <param name="attackInstanceId">当前攻击实例标识。</param>
        /// <param name="resultId">当前正式结果标识。</param>
        /// <param name="currentPosition">当前结束位置。</param>
        public ProjectileVisualTerminateContext(
            Map map,
            ThingDef projectileDef,
            string projectileThingId,
            string attackInstanceId,
            string resultId,
            Vector3 currentPosition)
        {
            Map = map;
            ProjectileDef = projectileDef;
            ProjectileThingId = projectileThingId;
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            CurrentPosition = currentPosition;
        }
    }
}
