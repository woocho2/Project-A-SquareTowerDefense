# 메뉴 엠블럼 선택 화면 (2026-11-18)

메뉴 씬의 EmblemPanel만 네이비·금색의 선택 창으로 정리했습니다. 스테이지 선택 카드·타이틀 배경·기존 메뉴 연결은 유지합니다.

- 왼쪽: 기존 문양 스프라이트 14종. 선택하면 금색 테두리·아이콘·체크 표시. 문양 또는 아래 선택 칸을 다시 누르면 해제하며 순서를 앞으로 당깁니다.
- 위쪽: 현재 스테이지·난이도, 선택 개수. 사용자 최신 요청에 따라 노말 4개·하드 6개를 선택합니다. 미완료일 때 SELECT 비활성화, 최대 선택 개수 초과 피드백.
- 아래쪽: 선택한 문양 목록과 남은 개수/선택 완료 안내.
- 오른쪽: 실제 SynergyRecipes.Pick과 같은 순서의 예상 시너지 타워. 얼굴·이름·일치 문양 수, 재료 중 선택한 문양 강조. 3개를 모두 골라야만 등장한다는 뜻이 아니라 현재 게임의 겹침 우선순위대로 표시합니다.
- SELECT: 중복 입장을 막고, 해당 프레임에 가벼운 Loading... 덮개를 표시합니다. 두 프레임 뒤 로딩 씬 자체도 LoadSceneAsync로 읽습니다. 이후 기존 타워 도감형 로딩 화면이 목적지 씬을 읽습니다. 모든 로딩을 완전히 무정지로 보장하는 검증은 아닙니다.

## 시너지 칸

메뉴를 구성할 때 각 스테이지의 실제 시너지 타일 수를 읽어 m_stageSynergyCapacity에 저장했습니다. 현재 Stage1은 6칸, Stage2~10은 0칸입니다. 칸이 없는 스테이지는 예상 0기와 안내를 표시합니다. 다른 스테이지에 시너지 칸을 추가하면 이 설정도 갱신해야 합니다. 스테이지나 게임의 조합·소환 규칙은 바꾸지 않았습니다.

## 검증과 미리보기

[선택 전](selection-empty.png) / [노말 4개 완료](selection-ready.png) / [하드 6개 완료](selection-hard-ready.png) / [SELECT 직후](select-immediate-loading.png)

최신 Unity PlayMode 41항목 통과: 실제 포인터 클릭, 노말 4개·하드 6개 칸 표시와 선택 제한, 체크·개수 갱신, 초과 선택 방지, 재클릭 및 선택 칸 해제, 순서 압축, 하드에서 노말 전환 시 앞의 4개 유지, 뒤로/재열기, 실제 생성된 시너지와 예상 목록 일치, SELECT 프레임의 로딩 덮개, 씬 로드 전 화면 갱신, 문양 전달, 중복 입장 방지. 텍스트 넘침 0·새 메뉴/로딩 오류 0. selection-16x10.png는 최초 제작 당시 참고 이미지입니다.

기존 Stage1 TowerFactory persistent parent 오류 6건은 existingErrors에 따로 기록했으며 이번에도 수정하지 않았습니다. resolved-capture-* 파일은 16:10 오프스크린 캡처 이후의 포인터 좌표 검사 문제를 해결한 도구 진단 기록입니다. 최종 검증에서는 정상 해상도 포인터 클릭을 확인했습니다.

## 파일과 복원

실제 변경: Assets/1. Scenes/2. Menu.unity, EmblemSelectPanel.cs, SceneFlow.cs.
신규: SceneTransitionOverlay.cs, EmblemMenuSetup.cs, EmblemMenuVerification.cs 및 각 메타.
백업: ArtBackups/EmblemMenu_20261118. 안전 확인용 LoadingSceneController.cs도 포함해 기존 4파일과 메타 4파일을 보관했습니다. LoadingSceneController는 이번 작업에서 변경하지 않았습니다. 원본 메타는 모두 해시가 같습니다. 문양 PNG·슬라이스·카탈로그는 바꾸지 않았습니다.

Unity를 종료하고 manifest.json의 OriginalFiles에 적힌 파일만 원래 상대 경로로 복사하면 작업 전으로 돌아갑니다. 이후 같은 파일에 추가한 수정은 먼저 따로 보관하세요. 새 스크립트를 제거할 경우 같은 이름의 메타도 함께 처리하세요. 전체 git 복원은 필요하지 않습니다.

Tools > SquareTD > 엠블럼 선택 화면 적용은 메뉴 씬의 EmblemPanel 내부 배치를 다시 만듭니다. 직접 꾸민 뒤 재적용할 때는 변경을 따로 보관하세요. 저장하지 않은 메뉴 씬 수정이 있으면 적용하지 않습니다. 스테이지의 타일 수는 읽기만 하고 스테이지 씬은 저장하지 않습니다.

전체 스테이지 클리어·배포 빌드는 수행하지 않았으며 커밋하지 않았습니다.

선택 수 변경 직전 백업은 ArtBackups/DeckCounts_20261118, 해당 변경의 검증·미리보기·파일 해시는 ArtPreviews/DeckCounts_20261118입니다. 메뉴 씬의 여분 선택 칸 두 개를 제거하고 슬롯 배열 6개·초기 노말 표시 4개를 Unity API로 저장했습니다. 이후 화면 재적용도 EmblemSelectPanel.NormalCount/HardCount를 사용합니다.
