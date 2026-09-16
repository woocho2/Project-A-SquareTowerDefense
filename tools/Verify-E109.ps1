$projectRoot = "d:/MyGitHub/ProjectA/Project-A-SquareTowerDefense"
$meta = Get-Content "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Electric_Splash_Thunder.png.meta" -Raw
$anim = Get-Content "$projectRoot/Assets/10.Animation/Effect/E109.anim" -Raw
$ctrl = Get-Content "$projectRoot/Assets/10.Animation/Effect/E109.controller" -Raw
$prefab = Get-Content "$projectRoot/Assets/2. Prefab/4. Effect/E109.prefab" -Raw
$lib = Get-Content "$projectRoot/Assets/Resources/EffectLibrary.asset" -Raw

$texGuid = "8a07011090000000000000000000109e"
$animGuid = "7a07011090000000000000000000109e"
$ctrlGuid = "6a07011090000000000000000000109e"
$prefabGuid = "440ddc97d929a1142a442dd23c76628e"

Write-Host "Tex Meta GUID Check: " ($meta -match "guid: $texGuid")
Write-Host "Anim -> Tex Check: " ($anim -match $texGuid)
Write-Host "Ctrl -> Anim Check: " ($ctrl -match $animGuid)
Write-Host "Prefab -> Ctrl Check: " ($prefab -match $ctrlGuid)
Write-Host "Prefab -> Tex Check: " ($prefab -match $texGuid)
Write-Host "EffectLibrary -> Prefab Check: " ($lib -match $prefabGuid)
