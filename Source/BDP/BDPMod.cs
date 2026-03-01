using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BDP.Trigger;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BDP
{
    /// <summary>
    /// 模组入口——在游戏启动时应用所有Harmony Patch。
    /// StaticConstructorOnStartup确保在游戏加载完成后执行。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class BDPMod
    {
        static BDPMod()
        {
            var harmony = new Harmony("com.niwt.bdp");
            harmony.PatchAll();
            Log.Message("[BDP] BorderDefenseProtocol 已加载，Harmony Patch已应用。");

            // PMS模块注册：配置类型 → 模块工厂
            BDPModuleFactory.Register<BeamTrailConfig>(cfg => new TrailModule(cfg));
            BDPModuleFactory.Register<BDPGuidedConfig>(cfg => new GuidedModule(cfg));
            BDPModuleFactory.Register<BDPExplosionConfig>(cfg => new ExplosionModule(cfg));
            BDPModuleFactory.Register<BDPTrackingConfig>(cfg => new TrackingModule(cfg));

            VerifyEngineMethodIntegrity();
        }

        /// <summary>
        /// 引擎源码复制完整性校验（C1/C2缓解措施）。
        /// BDP复制了两个引擎方法的逻辑，此方法在mod加载时校验原版IL是否与开发时一致，
        /// 不一致则发出警告。仅在加载时执行一次，运行时零开销。
        /// </summary>
        private static void VerifyEngineMethodIntegrity()
        {
            // C1: Verb_LaunchProjectile.TryCastShot — BDP在Verb_BDPRangedBase.TryCastShotCore中复制
            CheckMethodIL(
                typeof(Verb_LaunchProjectile),
                "TryCastShot",
                BindingFlags.NonPublic | BindingFlags.Instance,
                ExpectedHash_TryCastShot,
                "Verb_LaunchProjectile.TryCastShot",
                "Verb_BDPRangedBase.TryCastShotCore"
            );

            // C2: Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget — BDP在Verb_BDPMelee中override
            // 校验ApplyMeleeDamageToTarget本身（稳定哨兵）
            CheckMethodIL(
                typeof(Verb_MeleeAttackDamage),
                "ApplyMeleeDamageToTarget",
                BindingFlags.NonPublic | BindingFlags.Instance,
                ExpectedHash_ApplyMelee,
                "Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget",
                "Verb_BDPMelee.ApplyMeleeDamageToTarget"
            );

            // C2补充: DamageInfosToApply的状态机MoveNext（精确检测伤害计算逻辑变化）
            // DamageInfosToApply是yield return迭代器，真正逻辑在编译器生成的状态机中
            CheckIteratorIL(
                typeof(Verb_MeleeAttackDamage),
                "DamageInfosToApply",
                ExpectedHash_DamageInfosToApply_MoveNext,
                "Verb_MeleeAttackDamage.DamageInfosToApply",
                "Verb_BDPMelee.ApplyMeleeDamageToTarget"
            );
        }

        // ── 预期哈希值，基于 RimWorld 1.6.4633 ──

        // C1: Verb_LaunchProjectile.TryCastShot() IL (1321 bytes)
        private const string ExpectedHash_TryCastShot = "E9D6DF3EA91240DFA4E0A944EAA53343124698B8A3FD168C26B6E09701718904";

        // C2: Verb_MeleeAttackDamage.ApplyMeleeDamageToTarget() IL (75 bytes) — 稳定哨兵
        private const string ExpectedHash_ApplyMelee = "2026A1BBD23772AD6D0FA7772BD2ED99FACC530C458F780A456C238B9CE691E6";

        // C2: <DamageInfosToApply>d__2.MoveNext() IL (1247 bytes) — 精确检测
        // 注意：编译器生成的状态机类名(d__2)可能在引擎重编译时变化，此时会触发误报
        private const string ExpectedHash_DamageInfosToApply_MoveNext = "F60DCFB4D2A999128A357E09AB715BF680BEDA07DAEED89C49C058BC01E8BBED";

        private static void CheckMethodIL(
            System.Type type, string methodName, BindingFlags flags,
            string expectedHash, string engineMethodName, string bdpMethodName)
        {
            var method = type.GetMethod(methodName, flags);
            if (method == null)
            {
                Log.Warning($"[BDP] 引擎校验：找不到 {engineMethodName}，BDP的 {bdpMethodName} 可能需要更新。");
                return;
            }

            var body = method.GetMethodBody();
            if (body == null)
            {
                Log.Warning($"[BDP] 引擎校验：无法读取 {engineMethodName} 的IL，跳过校验。");
                return;
            }

            byte[] il = body.GetILAsByteArray();
            string hash;
            using (var sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(il);
                hash = System.BitConverter.ToString(hashBytes).Replace("-", "");
            }

            if (expectedHash == "PENDING")
            {
                // 首次运行：输出哈希供开发者记录
                Log.Message($"[BDP] 引擎校验：{engineMethodName} 当前IL哈希 = {hash}（请记录到ExpectedHash常量）");
                return;
            }

            if (hash != expectedHash)
            {
                Log.Warning(
                    $"[BDP] ⚠ 引擎方法变化检测：{engineMethodName} 的IL哈希不匹配！\n" +
                    $"  预期（开发版本）: {expectedHash}\n" +
                    $"  实际（当前版本）: {hash}\n" +
                    $"  BDP的 {bdpMethodName} 是此方法的修改副本，可能与当前引擎版本存在行为偏差。\n" +
                    $"  请等待BDP更新，或在GitHub反馈此问题。"
                );
            }
        }

        /// <summary>
        /// 校验yield return迭代器方法的编译器生成状态机MoveNext的IL哈希。
        /// 迭代器方法的真正逻辑在嵌套类的MoveNext()中，而非方法本身。
        /// </summary>
        private static void CheckIteratorIL(
            Type type, string iteratorMethodName,
            string expectedHash, string engineMethodName, string bdpMethodName)
        {
            // 查找编译器生成的嵌套类：<MethodName>d__N
            var nestedType = type.GetNestedTypes(BindingFlags.NonPublic)
                .FirstOrDefault(t => t.Name.Contains($"<{iteratorMethodName}>"));

            if (nestedType == null)
            {
                // 状态机类名变化 = 引擎重编译，视为变化信号
                Log.Warning($"[BDP] 引擎校验：找不到 {engineMethodName} 的迭代器状态机，引擎可能已重构此方法。");
                return;
            }

            var moveNext = nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
            if (moveNext == null)
            {
                Log.Warning($"[BDP] 引擎校验：找不到 {nestedType.Name}.MoveNext，跳过校验。");
                return;
            }

            var body = moveNext.GetMethodBody();
            if (body == null) return;

            byte[] il = body.GetILAsByteArray();
            string hash;
            using (var sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(il);
                hash = BitConverter.ToString(hashBytes).Replace("-", "");
            }

            if (expectedHash == "PENDING")
            {
                Log.Message($"[BDP] 引擎校验：{nestedType.Name}.MoveNext 当前IL哈希 = {hash}");
                return;
            }

            if (hash != expectedHash)
            {
                Log.Warning(
                    $"[BDP] ⚠ 引擎方法变化检测：{engineMethodName}（迭代器）的IL哈希不匹配！\n" +
                    $"  状态机类: {nestedType.Name}\n" +
                    $"  预期: {expectedHash}\n" +
                    $"  实际: {hash}\n" +
                    $"  BDP的 {bdpMethodName} 是此方法的修改副本，可能与当前引擎版本存在行为偏差。"
                );
            }
        }
    }
}
