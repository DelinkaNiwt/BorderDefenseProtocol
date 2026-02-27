// IBDPInterceptModifier 已移除。
// 原因：阶段位置错误——拦截检查在 base.TickInterval() 内部发生，
// 此阶段在 base 之后执行，永远无法影响拦截结果。
// 如需穿透弹，需通过 Harmony patch CheckForFreeInterceptBetween 实现，届时重新设计此阶段。
namespace BDP.Trigger { }
