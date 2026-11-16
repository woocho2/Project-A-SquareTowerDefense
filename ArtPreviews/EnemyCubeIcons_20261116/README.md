# 적 큐브 프레임·면 배지

적용 파일:
- `Assets/4. Asset/4. Enemy/Enemy_State_Badges_Atlas.png`: 기존 크기·배치 유지. 자물쇠 검정, 모래시계 회색. 다른 네 칸과 전체 알파는 원본과 동일.
- `Assets/4. Asset/4. Enemy/Enemy_Boss_Gimmick_Badge.png`: 새 256×256 공용 왕관 배지, 투명 배경.
- `Assets/4. Asset/0. Common/Enemy_State_{Heal,Defense,Action,Mobility,Locked,Hourglass}.mat`: 기존 타워의 Board_Decoration_Thin 공유. Locked 검정, Hourglass 회색.
- 같은 폴더 `Enemy_State_BossGimmick.mat`: 새 보스 전용 표시 재질.
- `Assets/2. Prefab/2. Enemy.prefab`: 보스 표시 재질 연결 한 칸 추가.
- `EnemyCubeMeshVisual.cs`, `EnemyMovement.cs`, `EnemyController.cs`: 최종 보스에서만 자물쇠 면의 표시 재질 교체. 면 ID·효과·다른 면 유지. 일반/중간 적으로 풀 재사용하면 정상 재질로 복귀.
- `Assets/Editor/EnemyCubeVisualSetup.cs`: 재적용 메뉴와 별도 씬 렌더·표시 교체 검증. 이미 적용된 상태이므로 실행할 필요 없음.

`unity-material-preview.png`는 실제 적 큐브 메시·셰이더·재질을 별도 Unity 프로젝트에서 렌더한 비교입니다. 원본 프로젝트를 점유한 밸런스 시뮬레이션은 중단하지 않았습니다.
파일 검증: 크기, 여백, 기존 .meta 보존, 다른 네 칸 픽셀 보존, 원래 알파 보존, Thin 참조 확인. `verification.json` 참고.
Unity 검증: 여섯 면의 보스 표시 교체와 논리 ID 유지, 일반 적으로 재사용 시 복귀 확인. `material-swap-validation.json` 참고.
프로젝트 C# 컴파일 오류 0개, 기존 PlayerTurnPanel 경고 3개. 메인 씬의 실제 PlayMode 화면은 이번 작업에서 확인하지 않았습니다.

제작: 내장 image_gen 사용. `state-atlas-prompt.txt`, `boss-badge-prompt.txt`에 전달 프롬프트 보관.
생성된 atlas 시안은 채택하지 않았으며, 원본 atlas의 두 칸만 색상 변경했습니다. 왕관은 생성 결과의 흰색 실루엣을 사용하고 기존 배지와 같은 원형 바탕·테두리로 마감했습니다. `prepare-assets.ps1` 참고.
`verify-files.ps1`은 프로젝트 루트에서 실행합니다.
백업: `ArtBackups/EnemyCubeVisual_20261116_01`.
