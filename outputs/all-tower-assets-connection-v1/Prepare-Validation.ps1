$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskProject=Join-Path $PSScriptRoot 'UnityValidation'
foreach($taskFolder in @('Assets/Scripts','Assets/Editor','Assets/Characters','Assets/Resources','Packages','ProjectSettings')){[void][IO.Directory]::CreateDirectory((Join-Path $taskProject $taskFolder))}
foreach($taskFile in @('TowerVisual.cs','TowerCharacterCatalog.cs','SynergyCharacterCatalog.cs')){
    foreach($taskSuffix in @('','.meta')){Copy-Item -LiteralPath (Join-Path $taskRoot ('Assets/3. Scripts/01. Tower/'+$taskFile+$taskSuffix)) -Destination (Join-Path $taskProject ('Assets/Scripts/'+$taskFile+$taskSuffix))}
}
foreach($taskSuffix in @('','.meta')){Copy-Item -LiteralPath (Join-Path $taskRoot ('Assets/3. Scripts/01. Tower/02. TowerData/TowerDataSO.cs'+$taskSuffix)) -Destination (Join-Path $taskProject ('Assets/Scripts/TowerDataSO.cs'+$taskSuffix))}
Copy-Item -LiteralPath (Join-Path $taskRoot 'outputs/sword-tower-runtime/VisualCheckAdapters.cs') -Destination (Join-Path $taskProject 'Assets/Scripts/VisualCheckAdapters.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AllTowerVisualCheck.cs') -Destination (Join-Path $taskProject 'Assets/Editor/AllTowerVisualCheck.cs')
foreach($taskAsset in @('TowerCharacterCatalog.asset','SynergyCharacterCatalog.asset')){foreach($taskSuffix in @('','.meta')){Copy-Item -LiteralPath (Join-Path $taskRoot ('Assets/Resources/'+$taskAsset+$taskSuffix)) -Destination (Join-Path $taskProject ('Assets/Resources/'+$taskAsset+$taskSuffix))}}
foreach($taskFolder in @('Assets/4. Asset/2. Tower/CubeConcepts','Assets/4. Asset/2. Tower/CubeConcepts/Remaining_v1','Assets/4. Asset/2. Tower/SynergyCube/Concepts_v3')){
    foreach($taskFile in (Get-ChildItem -LiteralPath (Join-Path $taskRoot $taskFolder) -File -Filter '*.png')){foreach($taskSuffix in @('','.meta')){Copy-Item -LiteralPath ($taskFile.FullName+$taskSuffix) -Destination (Join-Path $taskProject ('Assets/Characters/'+$taskFile.Name+$taskSuffix))}}
}
foreach($taskFile in @('Bronze.png','Silver.png','Gold.png','Mithril.png','Diamond.png','Tower_White_Background.png','Tower_Emblem_White_Atlas.png')){foreach($taskSuffix in @('','.meta')){Copy-Item -LiteralPath (Join-Path $taskRoot ('Assets/4. Asset/2. Tower/'+$taskFile+$taskSuffix)) -Destination (Join-Path $taskProject ('Assets/Characters/'+$taskFile+$taskSuffix))}}
Copy-Item -LiteralPath (Join-Path $taskRoot 'Assets/2. Prefab/2. Tower/TowerBase.prefab') -Destination (Join-Path $taskProject 'Assets/TowerBase.prefab.txt')
Copy-Item -LiteralPath (Join-Path $taskRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $taskProject 'ProjectSettings/ProjectVersion.txt')
[IO.File]::WriteAllText((Join-Path $taskProject 'Packages/manifest.json'),'{"dependencies":{}}',[Text.UTF8Encoding]::new($false))
Write-Output $taskProject
