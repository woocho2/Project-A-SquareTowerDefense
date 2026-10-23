# 제출용 복구 전 백업

작업 직전 파일 61개와 각각의 .meta, 총 122개를 원래 상대 경로로 보관했습니다. 원본 SHA-256은 manifest.json에 있습니다.

- 명중 이펙트 프리팹 28개, 블루 투사체 Idle/Fly 애니메이션 28개
- Assets/1. Scenes/3. Stage1.unity
- 옛 UI 코드 TowerInfoPanel.cs, PlayerTurnPanel.cs, UIManager.cs, EnemyInfoPanel.cs

New 스크립트·에디터·테스트와 Assets/*.txt 문서는 수정하지 않았습니다. HEAD는 b5a2c4f5 (Version 0.2.3.1) 그대로이며 커밋하지 않았습니다.

## 새 게임 작업 상태로 복원

1. Unity 플레이 모드를 종료하고 에디터를 닫습니다.
2. 프로젝트 루트에서 아래 PowerShell을 실행합니다. manifest에 기록된 파일만 작업 전 내용으로 덮어씁니다. 이후 이 파일들에 추가한 수정도 덮어쓰므로 필요한 변경은 먼저 별도로 보관하세요.

```powershell
$submissionBackup = Join-Path (Get-Location) 'outputs/submission-backup'
$submissionManifest = Get-Content -LiteralPath (Join-Path $submissionBackup 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $submissionManifest.Files) {
    $source = Join-Path $submissionBackup $entry.Path
    $destination = Join-Path (Get-Location) $entry.Path
    Copy-Item -LiteralPath $source -Destination $destination -Force
}
```

3. Unity를 다시 엽니다. 애니메이션의 OrbVisual 경로, 새 HitEffect가 붙은 프리팹, 작업 전 씬과 옛 UI 코드까지 복원됩니다. 새 매니저 활성 여부도 백업 시점 그대로이므로 개발 재개 시 직접 켜 주세요.

git reset --hard나 전체 checkout은 필요하지 않습니다. 이 백업에는 커밋 전 수정 내용도 있습니다.

## 제출 상태 변경·확인

- HEAD로 되돌린 56개 전체 경로: reverted-files.txt
- 씬과 옛 코드 변경 설명: scene-changes.txt
- 백업 해시·HEAD 일치·컴포넌트·애니메이션 경로 검증: verification.json
- 검증 재실행: 프로젝트 루트에서 powershell -NoProfile -ExecutionPolicy Bypass -File outputs/submission-verify.ps1

플레이에서 8마리 소환, 타워 생성, 턴 진행과 적 이동, 공격·처치·골드 지급, 2웨이브 진입을 확인했습니다. 타워 정보의 이름과 캐릭터 초상, 옵션 열기·계속하기도 확인했습니다. 마지막 플레이의 빨간 콘솔 오류는 0개였습니다. 기존 GameManager의 DontDestroyOnLoad 경고는 남아 있습니다.

사용자 최종 요청에 따라 다른 정보 패널의 완전 동작은 검증 범위에서 제외했습니다. 28종 전체의 이펙트 소멸·블루 프레임 애니메이션을 각각 실측하지는 않았으며, 해당 56개 에셋의 HEAD 일치와 옛 풀 컴포넌트·루트 커브 복원을 확인했습니다. 전체 스테이지 클리어와 배포 빌드는 수행하지 않았습니다.