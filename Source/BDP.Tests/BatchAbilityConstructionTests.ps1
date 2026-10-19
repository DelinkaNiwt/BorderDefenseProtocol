param([Parameter(Mandatory = $true)][ValidateSet('1.6', '1.7')][string]$RimWorldVersion)
$ErrorActionPreference = 'Stop'
# 读取实际程序集元数据，不加载游戏或依赖另一版本，验证原版授予／读档的构造契约。
$batchModRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$batchAssemblyPath = Join-Path $batchModRoot "$RimWorldVersion/Assemblies/BDP.Core.dll"
$batchStream = [IO.File]::OpenRead($batchAssemblyPath)
$batchReader = [System.Reflection.PortableExecutable.PEReader]::new($batchStream)
try {
    $batchMetadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($batchReader)
    $batchConstructors = @()
    foreach ($batchHandle in $batchMetadata.TypeDefinitions) {
        $batchDefinition = $batchMetadata.GetTypeDefinition($batchHandle)
        if ($batchMetadata.GetString($batchDefinition.Namespace) -ne 'BDP.Core.Abilities' -or
            $batchMetadata.GetString($batchDefinition.Name) -ne 'BdpBatchAbility') { continue }
        foreach ($batchMethodHandle in $batchDefinition.GetMethods()) {
            $batchMethod = $batchMetadata.GetMethodDefinition($batchMethodHandle)
            if ($batchMetadata.GetString($batchMethod.Name) -ne '.ctor' -or
                ($batchMethod.Attributes -band [Reflection.MethodAttributes]::MemberAccessMask) -ne [Reflection.MethodAttributes]::Public) { continue }
            $batchParameters = @($batchMethod.GetParameters() | ForEach-Object {
                $batchParameter = $batchMetadata.GetParameter($_)
                if ($batchParameter.SequenceNumber -gt 0) { $batchMetadata.GetString($batchParameter.Name) }
            })
            $batchConstructors += ($batchParameters -join ',')
        }
    }
    if ('pawn' -notin $batchConstructors) { throw '缺少传入角色的读档构造，原版能力列表不能恢复此类型。' }
    if ('pawn,def' -notin $batchConstructors) { throw '缺少传入角色和定义的能力工厂构造。' }
    Write-Output "BatchAbilityConstructionTests $RimWorldVersion PASS"
}
finally {
    $batchReader.Dispose()
    $batchStream.Dispose()
}
