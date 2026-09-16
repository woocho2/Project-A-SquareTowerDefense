 = 'd:/MyGitHub/ProjectA/Project-A-SquareTowerDefense'
 = Get-Content '/Assets/4. DotAsset/6. Effect/Effect_Wind_Splash_Typhoon.png.meta' -Raw
 = Get-Content '/Assets/10.Animation/Effect/E110.anim' -Raw
 = Get-Content '/Assets/10.Animation/Effect/E110.controller' -Raw
 = Get-Content '/Assets/2. Prefab/4. Effect/E110.prefab' -Raw
 = Get-Content '/Assets/2. Prefab/4. Effect/EffectLibrary.asset' -Raw

 = '8a07011100000000000000000000110e'
 = '7a07011100000000000000000000110e'
 = '6a07011100000000000000000000110e'
 = '520a011100000000000000000000110f'

Write-Host ('Tex Meta GUID Check: ' + ( -match ('guid: ' + )))
Write-Host ('Anim -> Tex Check: ' + ( -match ))
Write-Host ('Ctrl -> Anim Check: ' + ( -match ))
Write-Host ('Prefab -> Ctrl Check: ' + ( -match ))
Write-Host ('Prefab -> Tex Check: ' + ( -match ))
Write-Host ('EffectLibrary -> Prefab Check: ' + ( -match ))
