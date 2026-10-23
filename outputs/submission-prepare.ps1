$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$backup=Join-Path $root 'outputs/submission-backup'
if(Test-Path -LiteralPath (Join-Path $backup 'manifest.json')){throw 'Existing submission backup; do not overwrite'}
$git=Get-ChildItem -Path "$env:LOCALAPPDATA/GitHubDesktop/app-*/resources/app/git/cmd/git.exe" | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if((& $git log -1 --format='%s') -ne 'Version 0.2.3.1'){throw 'Unexpected HEAD'}
$effects=@(Get-ChildItem -LiteralPath (Join-Path $root 'Assets/4. Asset/6. Effect') -File | Where-Object {$_.Name -match '^(Hit_.+_30_v4|Sword_.+_30_v3)\.prefab$'})
$clips=@(Get-ChildItem -LiteralPath (Join-Path $root 'Assets/4. Asset/3. Projectile/Target') -File | Where-Object {$_.Name -match '_Projectile_Animation_(Idle|Fly)\.anim$'})
if($effects.Count -ne 28 -or $clips.Count -ne 28){throw 'Expected 28 effects + 28 clips'}
$restore=@($effects+$clips | ForEach-Object {$_.FullName.Substring($root.Length+1).Replace('\','/')})
$scene='Assets/1. Scenes/3. Stage1.unity'
$code='Assets/3. Scripts/05. UI/TowerInfoPanel.cs'
$paths=@($restore)+@($scene,$code)
$records=@()
foreach($path in $paths){foreach($p in @($path,($path+'.meta'))){
 $source=Join-Path $root $p;$dest=Join-Path $backup $p
 if(!(Test-Path -LiteralPath $source)){throw "Missing backup source $p"}
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest))|Out-Null
 [IO.File]::Copy($source,$dest,$false)
 $records+=[pscustomobject]@{Path=$p;Hash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash}
}}
$protected=@()
foreach($dir in @('Assets/3. Scripts/New','Assets/Editor/Tests/New','Assets/Editor/New')){
 if(Test-Path -LiteralPath (Join-Path $root $dir)){ $protected+=Get-ChildItem -LiteralPath (Join-Path $root $dir) -File -Recurse }
}
$protected+=Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Filter '*.txt' -File
$protectedHashes=@($protected | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
[pscustomobject]@{Head=(& $git rev-parse HEAD);RestoredFromHead=$restore;Files=$records;Protected=$protectedHashes}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
@'
# 제출용 복구 전 백업

에셋 56개, Stage1 씬, 옛 TowerInfoPanel.cs와 각각의 .meta를 작업 직전 내용으로 보관했습니다. 새 스크립트·테스트·문서는 변경하지 않습니다.

## 새 게임 작업 상태로 돌아가기

1. Unity 플레이 모드를 종료하고 에디터를 닫습니다.
2. 프로젝트 루트의 PowerShell에서 아래를 실행합니다. 백업된 파일만 덮어쓰며 새 코드나 다른 작업은 삭제하지 않습니다.

```powershell
$submissionBackup = Join-Path (Get-Location) 'outputs/submission-backup'
$submissionManifest = Get-Content -LiteralPath (Join-Path $submissionBackup 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $submissionManifest.Files) {
    $source = Join-Path $submissionBackup $entry.Path
    $destination = Join-Path (Get-Location) $entry.Path
    Copy-Item -LiteralPath $source -Destination $destination -Force
}
```

3. Unity를 다시 엽니다. Stage1 씬과 카탈로그 로드 이름도 작업 전 상태로 복구됩니다. 새 매니저 활성화 여부는 백업 시점 그대로이므로 필요하면 수업 진행에 맞게 켭니다.
4. Git의 HEAD는 제출용 복구 기준이며, 위 복사 방식은 미커밋 새 작업 내용까지 되살립니다. `git reset --hard`는 사용하지 않습니다.

복구 파일 전체 목록과 원본 SHA-256: manifest.json.
'@|Set-Content -LiteralPath (Join-Path $backup 'README.md') -Encoding UTF8
& $git checkout HEAD -- @restore
if($LASTEXITCODE -ne 0){throw 'Scoped git checkout failed'}
$s=[IO.File]::ReadAllText((Join-Path $root $scene))
$beforeScene=$s
$prefabs=@(
 @{Name='Manager';Guid='1259c084bc3ec6d4d966ec8c71d6875c';RootId='8521268422875413209'},
 @{Name='Pool';Guid='8156ef0932ada0f4493f3267e52967bc';RootId='7537974313177265303'},
 @{Name='UICanvas';Guid='2a2dc2c1027adca49894008fc234f54c';RootId='2519520974583011991'}
)
$sceneChanges=@()
foreach($p in $prefabs){
 $blocks=@([regex]::Matches($s,'(?ms)^--- !u!1001 &[^\r\n]+\r?\n.*?(?=^--- !u!|\z)')|Where-Object {$_.Value.Contains('m_SourcePrefab: {fileID: 100100000, guid: '+$p.Guid+', type: 3}')})
 if($blocks.Count -ne 1){throw ('Expected one prefab instance '+$p.Name)}
 $old=$blocks[0].Value
 $pattern='(?m)(    - target: \{fileID: '+$p.RootId+', guid: '+$p.Guid+', type: 3\}\r?\n      propertyPath: m_IsActive\r?\n      value: )([01])'
 if([regex]::IsMatch($old,$pattern)){$new=[regex]::Replace($old,$pattern,'${1}1')}else{
  $entry='    - target: {fileID: '+$p.RootId+', guid: '+$p.Guid+", type: 3}`n      propertyPath: m_IsActive`n      value: 1`n      objectReference: {fileID: 0}`n"
  $new=$old.Replace('    m_RemovedComponents:',($entry+'    m_RemovedComponents:'))
 }
 if($new -cne $old){$s=$s.Replace($old,$new);$sceneChanges+=$p.Name+' enabled explicitly'}
}
$oldInputGuid=[regex]::Match([IO.File]::ReadAllText((Join-Path $root 'Assets/3. Scripts/08.Common/BoardInputController.cs.meta')),'guid: ([a-f0-9]{32})').Groups[1].Value
$inputBlock=[regex]::Match($s,'(?ms)^--- !u!114 &2112983931\r?\n.*?(?=^--- !u!|\z)').Value
if(!$inputBlock.Contains($oldInputGuid)){throw 'BoardInputController is not the expected old script; inspect before changing'}
if($s -cne $beforeScene){[IO.File]::WriteAllText((Join-Path $root $scene),$s,[Text.UTF8Encoding]::new($false))}
$c=[IO.File]::ReadAllText((Join-Path $root $code))
$c=$c.Replace('Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog")','Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog_Old")').Replace('Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog")','Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog_Old")')
[IO.File]::WriteAllText((Join-Path $root $code),$c,[Text.UTF8Encoding]::new($false))
$sceneChanges|Set-Content -LiteralPath (Join-Path $backup 'scene-changes.txt') -Encoding UTF8
'Backed up '+$records.Count+' files; restored 56 assets from HEAD; enabled old scene roots; fixed two old portrait load names.'
