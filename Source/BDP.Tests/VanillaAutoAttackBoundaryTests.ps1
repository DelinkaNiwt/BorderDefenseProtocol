# 使用实际核心程序集和 Harmony（补丁框架）验证自动攻击边界，不启动游戏。
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$managed = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies\0Harmony.dll'
$dirs = @($managed, (Join-Path $modRoot '1.6\Assemblies'), (Split-Path $harmony))
$resolver = [ResolveEventHandler] {
    param($sender, $args)
    foreach ($dir in $dirs) {
        $path = Join-Path $dir (([Reflection.AssemblyName]::new($args.Name)).Name + '.dll')
        if (Test-Path -LiteralPath $path) { return [Reflection.Assembly]::LoadFrom($path) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $corePath = Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'
    [void][Reflection.Assembly]::LoadFrom($harmony)
    [void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
    [void][Reflection.Assembly]::LoadFrom($corePath)
    $sample = @'
using System;
using System.Reflection;
using System.Runtime.Serialization;
using BDP.Core.Verbs;
using HarmonyLib;
using RimWorld;
using Verse;

/// <summary>只替代需要完整地图的选择依赖，运行实际 BDP 查询及原版接入补丁。</summary>
public static class VanillaAutoAttackBoundarySample
{
    /// <summary>真实核心程序集与内部攻击入口类型。</summary>
    private static readonly Assembly Core = typeof(BdpVerb_Shoot).Assembly;
    /// <summary>只通过反射调用内部入口，不新增正式程序集测试接口。</summary>
    private static readonly Type Surface = Core.GetType("BDP.Core.AttackExecution.AttackExecutionSurfaceAccess");
    /// <summary>样本默认远程攻击。</summary>
    private static readonly BdpVerb_Shoot Ranged = new BdpVerb_Shoot();
    /// <summary>样本默认近战攻击。</summary>
    private static readonly Verb Melee = new BdpVerb_MeleeAttackDamage();
    /// <summary>原方法接收的上下文，验证查询没有覆盖引用。</summary>
    private static object Context;
    /// <summary>准备默认近战的调用次数，验证原版门槛和显式候选不被越过。</summary>
    private static int Prepared;
    /// <summary>替代只在游戏进程中可用的完整忙碌状态读取。</summary>
    private static bool Busy;

    /// <summary>访问真实字段或属性，缺失时明确失败。</summary>
    private static void Set(object value, string name, object fieldValue)
    {
        FieldInfo field = AccessTools.Field(value.GetType(), name);
        if (field != null) field.SetValue(value, fieldValue);
        else AccessTools.Property(value.GetType(), name).SetValue(value, fieldValue, null);
    }

    /// <summary>替代地图服务解析，仅用于本进程样本。</summary>
    public static bool Resolve(ref object __result)
    {
        __result = FormatterServices.GetUninitializedObject(Core.GetType("BDP.Core.AttackExecution.AttackExecutionService"));
        return false;
    }

    /// <summary>选择已有候选，不执行正式攻击或地图访问。</summary>
    public static bool Select(object[] __args, ref bool __result)
    {
        __args[3] = null; __args[4] = null; __args[5] = Ranged;
        __result = true; return false;
    }

    /// <summary>替代人物工作禁用查询，不改变任何游戏进程。</summary>
    public static bool Violent(ref bool __result) { __result = false; return false; }

    /// <summary>保留忙碌门槛两端，避免离线测试触发模组配置文件加载。</summary>
    public static bool IsBusy(ref bool __result) { __result = Busy; return false; }

    /// <summary>样本视为已经入图，不构造离线进程没有的地图列表。</summary>
    public static bool IsSpawned(ref bool __result) { __result = true; return false; }

    /// <summary>离线进程只跳过 DefOf（定义绑定表）的游戏启动器，不替代攻击实现。</summary>
    public static bool SkipDefInitialization() { return false; }

    /// <summary>仅在近战接入测试中返回已准备的攻击候选。</summary>
    public static bool Prepare(object[] __args, ref bool __result)
    {
        Prepared++; __args[2] = Melee; __result = true; return false;
    }

    /// <summary>异常即失败，保留精确场景说明。</summary>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    /// <summary>验证查询返回值、会话引用和近战任务身份，补丁在 finally（最终清理）中撤销。</summary>
    public static string Run()
    {
        var h = new Harmony("bdp.tests.vanilla_auto_attack_boundary");
        try
        {
            MethodInfo query = AccessTools.Method(Surface, "TryGetAutoRangedVerb");
            Check(query.GetParameters().Length == 4, "远程查询仍含旧会话清空返回口");
            Type meleePatch = Core.GetType("BDP.Patches.Patch_Pawn_MeleeVerbs_TryMeleeAttack");
            MethodInfo prefix = AccessTools.Method(meleePatch, "Prefix");
            Check(prefix.ReturnType == typeof(void), "自动近战仍跳过原版方法或接管任务");
            h.Patch(AccessTools.Method(Surface, "ResolveEntry"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "Resolve"));
            h.Patch(AccessTools.Method(Surface, "TryResolveAutoPrimaryVerb"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "Select"));
            h.Patch(AccessTools.Method(typeof(Pawn), "WorkTagIsDisabled"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "Violent"));
            h.Patch(AccessTools.PropertyGetter(typeof(Pawn_StanceTracker), "FullBodyBusy"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "IsBusy"));
            h.Patch(AccessTools.PropertyGetter(typeof(Thing), "Spawned"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "IsSpawned"));
            h.Patch(AccessTools.Method(typeof(DefOfHelper), "EnsureInitializedInCtor"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "SkipDefInitialization"));
            Pawn pawn = (Pawn)FormatterServices.GetUninitializedObject(typeof(Pawn));
            Pawn_JobTrackerPlaceholder(pawn);
            Ranged.verbProps = new VerbProperties();
            Context = FormatterServices.GetUninitializedObject(Core.GetType("BDP.Core.AttackExecution.AttackContextSnapshot"));
            Set(Ranged, "hostAttackContextSnapshot", Context);
            Set(Ranged, "HostSessionToken", Activator.CreateInstance(Core.GetType("BDP.Core.AttackExecution.AttackSessionToken")));
            object originalToken = AccessTools.Property(typeof(BdpVerb_Shoot), "HostSessionToken").GetValue(Ranged, null);
            Thing target = (Thing)FormatterServices.GetUninitializedObject(typeof(Thing));
            foreach (Thing requested in new Thing[] { null, target })
            {
                object[] args = { pawn, false, requested, null };
                Check((bool)query.Invoke(null, args) && ReferenceEquals(args[3], Ranged), "旧上下文使远程候选丢失");
                Check(ReferenceEquals(AccessTools.Field(typeof(BdpVerb_Shoot), "hostAttackContextSnapshot").GetValue(Ranged), Context), "远程查询改写冻结上下文");
                Check(ReferenceEquals(AccessTools.Property(typeof(BdpVerb_Shoot), "HostSessionToken").GetValue(Ranged, null), originalToken), "远程查询改写攻击令牌");
            }
            Ranged.verbProps.onlyManualCast = true;
            object[] manualArgs = { pawn, false, target, null };
            Check(!(bool)query.Invoke(null, manualArgs) && manualArgs[3] == null, "自动查询越过原版仅限手动门槛");
            manualArgs[1] = true;
            Check((bool)query.Invoke(null, manualArgs) && ReferenceEquals(manualArgs[3], Ranged), "显式允许手动武器时未返回候选");
            Ranged.verbProps.onlyManualCast = false;
            MethodInfo matches = AccessTools.Method(typeof(BdpVerb_Shoot), "MatchesAutomaticCastTarget");
            LocalTargetInfo oldCell = new LocalTargetInfo(new IntVec3(1, 0, 1));
            Set(Ranged, "sessionTarget", oldCell);
            Check((bool)matches.Invoke(Ranged, new object[] { oldCell }), "同格目标未保留续射");
            Check(!(bool)matches.Invoke(Ranged, new object[] { new LocalTargetInfo(new IntVec3(2, 0, 1)) }), "不同格目标误复用旧会话");
            Set(Ranged, "sessionTarget", new LocalTargetInfo(target));
            Check((bool)matches.Invoke(Ranged, new object[] { new LocalTargetInfo(target) }), "同实体目标未保留续射");
            Thing other = (Thing)FormatterServices.GetUninitializedObject(typeof(Thing));
            Check(!(bool)matches.Invoke(Ranged, new object[] { new LocalTargetInfo(other) }), "新实体目标误复用旧会话");
            h.Patch(AccessTools.Method(Surface, "TryPrepareAutoMelee"), new HarmonyMethod(typeof(VanillaAutoAttackBoundarySample), "Prepare"));
            Set(pawn, "kindDef", new PawnKindDef { canMeleeAttack = true });
            Pawn_StanceTracker stances = new Pawn_StanceTracker(pawn);
            Set(pawn, "stances", stances);
            var activeRanged = new BdpVerb_FormalHostShoot { caster = pawn, verbProps = new VerbProperties() };
            Set(activeRanged, "hostAttackContextSnapshot", Context);
            MethodInfo castSession = AccessTools.Method(typeof(BdpVerb_FormalHostShoot), "TryPrepareCastSession");
            Set(activeRanged, "state", VerbState.Bursting);
            Check(!(bool)castSession.Invoke(activeRanged, new object[] { new LocalTargetInfo(target) }), "实际起手覆盖正在连发的会话");
            Set(activeRanged, "state", VerbState.Idle);
            Busy = true;
            Check(!(bool)castSession.Invoke(activeRanged, new object[] { new LocalTargetInfo(target) }), "实际起手越过原版忙碌门槛");
            Check(ReferenceEquals(AccessTools.Field(typeof(BdpVerb_Shoot), "hostAttackContextSnapshot").GetValue(activeRanged), Context), "忙碌起手清掉了正在使用的上下文");
            Busy = false;
            var sourceJob = new Verse.AI.Job { def = new JobDef(), expiryInterval = 1, maxNumMeleeAttacks = 1, checkOverrideOnExpire = true };
            Set(pawn.jobs, "curJob", sourceJob);
            Pawn_MeleeVerbs meleeVerbs = new Pawn_MeleeVerbs(pawn);
            object[] meleeArgs = { meleeVerbs, target, null };
            prefix.Invoke(null, meleeArgs);
            Check(Prepared == 1 && ReferenceEquals(meleeArgs[2], Melee), "原版合法近战请求未注入默认近战: prepared=" + Prepared + ", spawned=" + target.Spawned);
            Check(ReferenceEquals(pawn.CurJob, sourceJob) && sourceJob.expiryInterval == 1
                && sourceJob.maxNumMeleeAttacks == 1 && sourceJob.checkOverrideOnExpire, "自动近战改写原版任务、期限或反击约束");
            meleeArgs[2] = Ranged;
            prefix.Invoke(null, meleeArgs);
            Check(Prepared == 1 && ReferenceEquals(meleeArgs[2], Ranged), "显式攻击被替换");
            JobDefOf.AttackMelee = sourceJob.def;
            sourceJob.playerForced = true;
            sourceJob.targetA = target;
            meleeArgs[2] = null;
            prefix.Invoke(null, meleeArgs);
            Check(Prepared == 1 && meleeArgs[2] == null, "玩家原版强制近战被接管");
            sourceJob.playerForced = false;
            Busy = true;
            meleeArgs[2] = null;
            prefix.Invoke(null, meleeArgs);
            Check(Prepared == 1 && meleeArgs[2] == null, "越过原版忙碌门槛");
            Busy = false;
            pawn.kindDef.canMeleeAttack = false;
            meleeArgs[2] = null;
            prefix.Invoke(null, meleeArgs);
            Check(Prepared == 1 && meleeArgs[2] == null, "越过原版禁止近战门槛");
            return "PASS: 真实远程查询保留候选且不改会话；近战只注入默认攻击，保留原版方法、任务身份及显式攻击／禁止近战门槛。";
        }
        finally { h.UnpatchAll(h.Id); }
    }

    /// <summary>构造空任务跟踪器供只读 CurJob（当前任务）属性访问，不运行任务或地图。</summary>
    private static void Pawn_JobTrackerPlaceholder(Pawn pawn)
    {
        Set(pawn, "jobs", FormatterServices.GetUninitializedObject(typeof(Verse.AI.Pawn_JobTracker)));
    }
}
'@
    Add-Type -TypeDefinition $sample -ReferencedAssemblies @($harmony, $corePath, (Join-Path $managed 'Assembly-CSharp.dll'), (Join-Path $managed 'UnityEngine.CoreModule.dll'), (Join-Path $managed 'Unity.Mathematics.dll'), 'System.Core.dll')
    [VanillaAutoAttackBoundarySample]::Run()
}
finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }
