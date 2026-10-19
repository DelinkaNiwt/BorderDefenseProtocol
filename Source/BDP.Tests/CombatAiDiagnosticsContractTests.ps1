# 使用 Windows PowerShell（.NET Framework）验证真实程序集签名，不启动游戏或调用业务方法。
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$gameManaged = 'C:\NiwtGames\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$assemblyDirs = @(
    $gameManaged,
    (Join-Path $modRoot '1.6\Assemblies'),
    (Join-Path $modRoot '1.6\Development\Assemblies'),
    'C:\NiwtGames\Steam\steamapps\workshop\content\294100\2009463077\1.6\Assemblies'
)
$resolveAssembly = [ResolveEventHandler] {
    param($sender, $eventArgs)
    $fileName = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name + '.dll'
    foreach ($dir in $assemblyDirs) {
        $file = Join-Path $dir $fileName
        if (Test-Path -LiteralPath $file) { return [Reflection.Assembly]::LoadFrom($file) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolveAssembly)
try {
    $game = [Reflection.Assembly]::LoadFrom((Join-Path $gameManaged 'Assembly-CSharp.dll'))
    $core = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Assemblies\BDP.Core.dll'))
    $development = [Reflection.Assembly]::LoadFrom((Join-Path $modRoot '1.6\Development\Assemblies\BDP.Development.dll'))
    $flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static,DeclaredOnly'
    $hooks = $development.GetType('BDP.Development.CombatAI.CombatAiDiagnosticsHooks', $true)
    $hookSource = Get-Content -LiteralPath (Join-Path $modRoot 'Source\BDP.Development\CombatAI\CombatAiDiagnosticsHooks.cs') -Raw -Encoding utf8
    $traceSource = Get-Content -LiteralPath (Join-Path $modRoot 'Source\BDP.Development\CombatAI\CombatAiTrace.cs') -Raw -Encoding utf8

    # 寻找真实类型，不执行任何静态构造器。
    function Find-Type([string]$name) {
        foreach ($assembly in @($game, $core, $development)) {
            $type = $assembly.GetType($name)
            if ($null -ne $type) { return $type }
        }
        throw "类型不存在: $name"
    }

    # 对齐 Harmony 对继承字段的查询方式。
    function Find-Member([Type]$type, [string]$name) {
        while ($null -ne $type) {
            $field = $type.GetField($name, $flags)
            if ($null -ne $field) { return $field }
            $property = $type.GetProperty($name, $flags)
            if ($null -ne $property) { return $property }
            $type = $type.BaseType
        }
        throw "读取成员不存在: $name"
    }

    # 读取口类型用于继续验证快照链，没有调用 Getter（读取器）。
    function Member-Type($member) {
        if ($member -is [Reflection.FieldInfo]) { return $member.FieldType }
        return $member.PropertyType
    }

    # 只对照运行时签名，不把源码包含字符串当作行为正确的证明。
    $contracts = [Collections.Generic.List[object]]::new()
    $decisionBlock = [regex]::Match($hookSource, '(?s)string\[\] decisions\s*=\s*\{(.*?)\};').Groups[1].Value
    foreach ($match in [regex]::Matches($decisionBlock, '"([^"]+)"')) {
        $contracts.Add(@($match.Groups[1].Value, 'TryGiveJob', 'DecisionBefore', 'DecisionAfter'))
    }
    foreach ($match in [regex]::Matches($hookSource, 'Install\("([^"]+)", "([^"]+)", (null|"[^"]+"), (null|"[^"]+")')) {
        $contracts.Add(@($match.Groups[1].Value, $match.Groups[2].Value,
            $match.Groups[3].Value.Trim('"'), $match.Groups[4].Value.Trim('"')))
    }
    if ($contracts.Count -ne 24) { throw "诊断目标数量不符，实际 $($contracts.Count)，预期 24" }
    $callbackCount = 0
    foreach ($contract in $contracts) {
        $type = Find-Type $contract[0]
        $methods = @($type.GetMethods($flags) | Where-Object { $_.Name -eq $contract[1] })
        if ($contract[1] -eq 'TryStartCastOn') {
            $methods = @($methods | Where-Object { $_.GetParameters().Count -eq 6 })
        }
        if ($methods.Count -ne 1) { throw "补丁目标无法唯一解析: $($contract[0]).$($contract[1])" }
        $target = $methods[0]
        $targetParameters = @{}
        foreach ($parameter in $target.GetParameters()) { $targetParameters[$parameter.Name] = $parameter.ParameterType }
        foreach ($callbackName in @($contract[2], $contract[3])) {
            if ($callbackName -eq 'null') { continue }
            $callback = $hooks.GetMethod($callbackName, $flags)
            if ($null -eq $callback -or $callback.ReturnType -ne [void]) { throw "观察器缺失或有返回值: $callbackName" }
            foreach ($parameter in $callback.GetParameters()) {
                if ($parameter.Name -eq '__args') { throw "观察器不得使用会隐式回写引用参数的 __args: $callbackName" }
                if ($parameter.Name -eq '__state' -or $parameter.Name -eq '__originalMethod') { continue }
                if ($parameter.ParameterType.IsByRef) { throw "观察器不应改写正式参数: $callbackName/$($parameter.Name)" }
                if ($parameter.Name -eq '__instance') { $actualType = $type }
                elseif ($parameter.Name -eq '__result') { $actualType = $target.ReturnType }
                elseif ($parameter.Name.StartsWith('___')) { $actualType = Member-Type (Find-Member $type $parameter.Name.Substring(3)) }
                elseif ($parameter.Name -match '^__(\d+)$') { $actualType = $target.GetParameters()[[int]$Matches[1]].ParameterType }
                else {
                    $actualType = $targetParameters[$parameter.Name]
                    if ($null -eq $actualType) { throw "目标没有命名参数: $callbackName/$($parameter.Name)" }
                }
                if ($actualType.IsByRef) { $actualType = $actualType.GetElementType() }
                if (-not $parameter.ParameterType.IsAssignableFrom($actualType)) {
                    throw "参数类型不匹配: $callbackName/$($parameter.Name): $actualType"
                }
            }
            $callbackCount++
        }
    }

    # 验证全部内部快照读取口，防止一进游戏才发现某个字段名失配。
    $trigger = Find-Type 'BDP.Core.Trigger.CompTriggerBody'
    $projection = Member-Type (Find-Member $trigger 'PublishedCombatProjection')
    $snapshot = Member-Type (Find-Member $projection 'Snapshot')
    $manager = Member-Type (Find-Member $trigger 'VerbHostManager')
    $bindings = Member-Type (Find-Member $manager 'bindings')
    $binding = $bindings.GetGenericArguments()[1]
    $ranged = Find-Type 'BDP.Core.Verbs.BdpVerb_Shoot'
    $cursor = Member-Type (Find-Member $ranged 'emissionCursor')
    $melee = Find-Type 'BDP.Core.Verbs.BdpVerb_MeleeAttackDamage'
    $checks = @(
        @($projection, @('ProjectionVersion')),
        @($snapshot, @('PrimaryRanged', 'PrimaryMelee')),
        @($binding, @('IsAvailable', 'RangedVerb', 'MeleeVerb')),
        @($ranged, @('HostSessionToken', 'sessionTarget', 'HostModuleSession', 'HostAttackContextSnapshot')),
        @($cursor, @('PendingVerbEmissionPlan', 'PendingWindowIndex', 'PendingEmissionConsumedCount')),
        @($melee, @('HostSessionToken', 'NextRuntimeStepIndex', 'PlanDispatchIntent', 'LastMeleeBurstShotCompleted', 'LastMeleeBurstShotHit')),
        @((Find-Type 'BDP.Core.AttackExecution.JobDriver_BdpMeleeAttackExecution'), @('castCount'))
    )
    foreach ($check in $checks) { foreach ($name in $check[1]) { $null = Find-Member $check[0] $name } }
    $resultType = Member-Type (Find-Member $snapshot 'PrimaryRanged')
    $null = Find-Member $resultType 'Id'

    # 语言包覆盖所有固定阶段与错误提示；每条中文文案均有语言键。
    [xml]$language = Get-Content -LiteralPath (Join-Path $modRoot '1.6\Development\Languages\ChineseSimplified (简体中文)\Keyed\CombatAiDiagnostics.xml') -Raw -Encoding utf8
    $stages = [regex]::Matches(($hookSource + $traceSource), '(?:Record|Write)\("([a-z_]+)"') |
        ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    foreach ($stage in @($stages) + 'Error') {
        if (-not $language.LanguageData.SelectSingleNode('BDP_Dev_CombatAI_' + $stage)) { throw "缺少语言键: $stage" }
    }
    # 实际调用日志计时读取口，覆盖主菜单尚无 Game（游戏对象）的启动时序。
    # 不构造游戏、计时器或地图，只创建无业务状态的对象并还原 Current.Game。
    $trace = $development.GetType('BDP.Development.CombatAI.CombatAiTrace', $true)
    $currentTick = $trace.GetProperty('CurrentTick', $flags)
    if ($null -eq $currentTick) { throw '诊断日志缺少可在主菜单读取的 CurrentTick 入口' }
    $current = Find-Type 'Verse.Current'
    $gameProperty = $current.GetProperty('Game', $flags)
    $previousGame = $gameProperty.GetValue($null, $null)
    try {
        $gameProperty.SetValue($null, $null, $null)
        if ($currentTick.GetValue($null, $null) -ne -1) { throw '主菜单日志应使用 tick=-1' }
        $emptyGame = [Runtime.Serialization.FormatterServices]::GetUninitializedObject((Find-Type 'Verse.Game'))
        $gameProperty.SetValue($null, $emptyGame, $null)
        if ($currentTick.GetValue($null, $null) -ne -1) { throw '计时器尚未创建时日志应使用 tick=-1' }
        $tickType = Find-Type 'Verse.TickManager'
        $tickManager = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($tickType)
        (Find-Member $tickType 'ticksGameInt').SetValue($tickManager, 12345)
        (Find-Member $emptyGame.GetType() 'tickManager').SetValue($emptyGame, $tickManager)
        if ($currentTick.GetValue($null, $null) -ne 12345) { throw '进游戏后日志应使用实际计时' }
    }
    finally { $gameProperty.SetValue($null, $previousGame, $null) }
    Write-Output "PASS: $($contracts.Count) 个真实补丁目标、$callbackCount 个只读回调、内部读取口、$(@($stages).Count + 1) 个阶段语言键及三个启动计时状态。未执行游戏业务或静态启动器。"
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveAssembly)
}
