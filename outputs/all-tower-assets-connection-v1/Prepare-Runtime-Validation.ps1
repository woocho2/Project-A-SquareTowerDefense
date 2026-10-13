$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskProject=Join-Path $PSScriptRoot 'UnityValidation'
# Move only the isolated harness adapters and duplicate scripts; the game workspace is not changed.
$taskRemovedDir=Join-Path $PSScriptRoot 'validation-adapter-backup'
[void][IO.Directory]::CreateDirectory($taskRemovedDir)
foreach($taskFile in (Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts') -File)){
    Move-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskRemovedDir $taskFile.Name) -Force
}
$taskSourceDir=Join-Path $taskRoot 'Assets/3. Scripts'
foreach($taskSource in (Get-ChildItem -LiteralPath $taskSourceDir -File -Recurse)){
    $taskRelative=$taskSource.FullName.Substring($taskSourceDir.Length+1)
    $taskTarget=Join-Path $taskProject ('Assets/GameScripts/'+$taskRelative)
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($taskTarget))
    Copy-Item -LiteralPath $taskSource.FullName -Destination $taskTarget -Force
}
[void][IO.Directory]::CreateDirectory((Join-Path $taskProject 'Assets/Plugins'))
$taskRsp=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Assembly-CSharp.rsp'))
$taskRuntimeDlls=@('Assembly-CSharp-firstpass.dll','Unity.InputSystem.dll','Unity.InputSystem.ForUI.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll','Unity.RenderPipelines.Core.Runtime.dll','Unity.RenderPipelines.Core.Runtime.Shared.dll','Unity.RenderPipelines.Universal.Runtime.dll','Unity.RenderPipelines.Universal.Config.Runtime.dll','Unity.RenderPipelines.Universal.2D.Runtime.dll','Unity.RenderPipelines.GPUDriven.Runtime.dll','Unity.Mathematics.dll','Unity.Collections.dll','Unity.Burst.dll')
foreach($taskDll in $taskRuntimeDlls){
    Copy-Item -LiteralPath (Join-Path $taskRoot ('Library/ScriptAssemblies/'+$taskDll)) -Destination (Join-Path $taskProject ('Assets/Plugins/'+$taskDll)) -Force
}
# Copy the actual GUID dependency graph of the prefab, projectiles, catalogs and effect library.
$taskMetaPaths=rg --files (Join-Path $taskRoot 'Assets') -g '*.meta'
$taskGuidPaths=@{}
foreach($taskMetaPath in $taskMetaPaths){
    $taskMeta=[IO.File]::ReadAllText($taskMetaPath)
    $taskGuid=[regex]::Match($taskMeta,'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
    if($taskGuid){$taskGuidPaths[$taskGuid]=$taskMetaPath.Substring(0,$taskMetaPath.Length-5)}
}
$taskQueue=New-Object 'System.Collections.Generic.Queue[string]'
foreach($taskSeed in @('Assets/2. Prefab/2. Tower/TowerBase.prefab','Assets/Resources/TowerCharacterCatalog.asset','Assets/Resources/SynergyCharacterCatalog.asset','Assets/2. Prefab/4. Effect/EffectLibrary.asset','Assets/Resources/CharacterRounded.mat')){
    $taskPath=Join-Path $taskRoot $taskSeed;if(Test-Path -LiteralPath $taskPath){$taskQueue.Enqueue($taskPath)}
}
foreach($taskPath in (Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets/Resources/Projectiles') -Filter '*.prefab')){$taskQueue.Enqueue($taskPath.FullName)}
$taskVisited=New-Object 'System.Collections.Generic.HashSet[string]'
while($taskQueue.Count){
    $taskPath=$taskQueue.Dequeue()
    if(!$taskVisited.Add($taskPath) -or !(Test-Path -LiteralPath $taskPath -PathType Leaf)){continue}
    if($taskPath -like '*.cs'){continue}
    $taskRelative=$taskPath.Substring($taskRoot.Length+1)
    $taskTarget=Join-Path $taskProject $taskRelative
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($taskTarget))
    Copy-Item -LiteralPath $taskPath -Destination $taskTarget -Force
    if(Test-Path -LiteralPath ($taskPath+'.meta')){Copy-Item -LiteralPath ($taskPath+'.meta') -Destination ($taskTarget+'.meta') -Force}
    if([IO.Path]::GetExtension($taskPath) -in @('.prefab','.asset','.mat','.anim','.controller','.shader','.cginc')){
        $taskText=[IO.File]::ReadAllText($taskPath)
        foreach($taskGuidMatch in [regex]::Matches($taskText,'guid: ([a-f0-9]{32})')){
            $taskGuid=$taskGuidMatch.Groups[1].Value
            if($taskGuidPaths.ContainsKey($taskGuid)){$taskQueue.Enqueue($taskGuidPaths[$taskGuid])}
        }
        foreach($taskInclude in [regex]::Matches($taskText,'#include "([^"\r\n]+)"')){
            $taskIncludePath=Join-Path ([IO.Path]::GetDirectoryName($taskPath)) $taskInclude.Groups[1].Value
            if(Test-Path -LiteralPath $taskIncludePath){$taskQueue.Enqueue([IO.Path]::GetFullPath($taskIncludePath))}
        }
    }
}
# The first geometry harness placed images in Characters. Remove only duplicated copies from this owned test project.
foreach($taskFile in (Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Characters') -File)){
    Move-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskRemovedDir $taskFile.Name) -Force
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'Assets/Resources/TowerDataCSV.csv') -Destination (Join-Path $taskProject 'Assets/Resources/TowerDataCSV.csv') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'ProjectSettings/TagManager.asset') -Destination (Join-Path $taskProject 'ProjectSettings/TagManager.asset') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'Assets/2. Prefab/2. Tower/TowerBase.prefab') -Destination (Join-Path $taskProject 'Assets/TowerBase.prefab.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AllTowerVisualCheck.cs') -Destination (Join-Path $taskProject 'Assets/Editor/AllTowerVisualCheck.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TowerRuntimeCheck.cs') -Destination (Join-Path $taskProject 'Assets/Editor/TowerRuntimeCheck.cs') -Force
$taskGamePackages=Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'Packages/manifest.json') | ConvertFrom-Json
$taskBuiltins=[ordered]@{}
foreach($taskDependency in $taskGamePackages.dependencies.PSObject.Properties){if($taskDependency.Name.StartsWith('com.unity.modules.')){$taskBuiltins[$taskDependency.Name]=$taskDependency.Value}}
[IO.File]::WriteAllText((Join-Path $taskProject 'Packages/manifest.json'),(@{dependencies=$taskBuiltins} | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
Write-Output ('Prepared real game scripts and '+$taskVisited.Count+' GUID dependencies in the isolated validation project.')
