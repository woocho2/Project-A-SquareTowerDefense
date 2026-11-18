# 로딩 화면 (2026-11-18)

기존 LoadingScene1·LoadingScene2를 같은 LoadingScreen.prefab으로 정리했습니다. 이전 Missing Script가 남아 있던 로딩 UI·LoadingManager는 이 두 로딩 씬에서 제거했습니다. 씬 GUID는 유지했습니다.

- 중앙: 일반 타워 56종 중 한 명을 무작위로 선택. 기존 Full 그림, 현재 이름, 역할 소개, 기본행동과 스킬을 표시합니다.
- 설명은 CSV·TowerStatCalculator·TowerDescriptionBuilder를 사용합니다. 브론즈·컬러 Lv.1, 추가 효과 없는 기본 상태 기준입니다. 별도 고정 수치나 임의의 캐릭터 이야기는 넣지 않았습니다.
- 배경은 기존 캐릭터 없는 위그드라실 그림을 재사용했습니다. PNG·카탈로그·Full/Face 슬라이스는 수정하지 않았습니다.
- 하단: 비조작 Slider, 실제 LoadSceneAsync.progress 기준 진행률. DOTween으로 부드럽게 채우며 100% 표시와 페이드 후 목적지를 활성화합니다. Loading...은 글자마다 시간차를 두고 가볍게 뜁니다.
- SceneFlow.Load/LoadStage/Reload를 사용하는 메뉴·스테이지 이동은 로딩을 경유합니다. 메뉴 목적지는 LoadingScene1, 나머지 목적지는 LoadingScene2입니다. 중복 클릭과 잘못된 씬 이름을 방지합니다.
- 직접 로딩 씬을 실행했을 때의 기본 목적지는 LoadingScene1=2. Menu, LoadingScene2=3. Stage1입니다. LoadingSceneController의 Fallback Scene에서 바꿀 수 있습니다. Minimum Display Time 기본값은 1.6초입니다.
- 프리팹과 컨트롤러의 직렬화 필드는 Unity 인스펙터에서 편집할 수 있습니다. Preview Only는 실제 이동을 멈추고 지정 타워를 확인하는 용도이며, 기본값은 꺼져 있습니다.

## 미리보기와 검증

[소드 마스터](loading-sword.png) / [발키리](loading-buff.png) / [16:10](loading-16x10.png)

- 파일/에디터 검증: 56종 이름·전신·기본/스킬 설명·텍스트 넘침 확인, 두 로딩 씬의 컨트롤러 및 Missing Script 검사 통과. 1920×1080, 1728×1080 캡처.
- 실제 Unity PlayMode: 로딩→메뉴, SceneFlow→로딩→Stage1, 진행률 역행 없음, 100% 표시 후 이동, Loading 글자 움직임, 중복 이동 방지, 선택 정보 유지, Time.timeScale=0에서도 완료 등 11항목 통과. 새 로딩 오류 0.
- Stage1에 이미 있던 TowerFactory의 persistent parent 오류 6건은 playmode-result.json의 existingErrors로 별도 기록했습니다. 이번 작업에서는 수정하지 않았습니다.
- 플레이 검증 후 Loading...의 글자 간격만 조정했습니다. 다시 컴파일하고 56종 전체 파일/화면 검증을 실행했습니다.
- 배포 빌드는 수행하지 않았고 커밋하지 않았습니다. layout-first-check-fixed.txt는 조정 전에 발견해 해결한 이름 영역 크기 검사 기록입니다.

## 파일·백업

변경: 두 로딩 씬, SceneFlow.cs. 신규: LoadingScreen.prefab, LoadingTowerProfile.cs, LoadingSceneController.cs, 두 에디터 적용/검증 도구와 각 .meta.

원본 3파일과 .meta 3파일은 ArtBackups/LoadingScene_20261118/Assets 아래 원래 경로로 보관했습니다. 기존 메타 3개는 해시가 동일합니다. 전체 파일 해시는 verification.json 및 백업의 manifest.json에 있습니다. 백업에 추가로 있는 LoadingScreen.prefab은 이번 제작 도중의 중간본이며 작업 전 원본이 아닙니다.

원복 시 Unity를 종료하고 manifest의 ChangedOriginalFiles에 적힌 파일만 백업에서 원래 위치로 복사하세요. 이후 같은 파일에 추가한 수정은 먼저 별도로 보관해야 합니다. 새 프리팹/스크립트는 원복 후 연결되지 않으므로 필요하면 해당 파일과 메타를 함께 제거하세요. 전체 git 복원은 필요하지 않습니다.

Tools > SquareTD > 로딩 화면 적용: 공용 프리팹과 두 씬 재생성. 두 로딩 씬을 다시 만들므로 직접 배치 변경 후에는 재적용 전에 따로 보관하세요.
Tools > SquareTD > 로딩 화면 검증: 전체 타워 파일 검사·캡처 후, 저장하지 않은 씬 변경과 실행 중인 밸런스 시뮬레이션이 없을 때 실제 전환까지 검증하고 원래 열린 씬으로 돌아갑니다.