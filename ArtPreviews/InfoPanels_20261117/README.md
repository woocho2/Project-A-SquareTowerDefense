# 정보 패널 정리

타워·타일·적의 공용 UI를 정리했습니다. 원본 백업: `ArtBackups/InfoPanels_20261117`.

- 공용 프리팹: `Assets/2. Prefab/UICanvas.prefab`.
- 300×760, 오른쪽/위 여백 20/124. 이름 24~26, 수치·설명 18, 항목명 16px 기준.
- 티어 강화·수동 합성 UI 제거. 타워 조작, 타일/적 목록과 상세, 스크롤, 패널 전환을 연결했습니다.
- 화면 높이에 맞춰 Canvas가 스케일되며, 기존 한글 폰트에 기호용 fallback을 연결했습니다.
- `scene-bindings.txt`: Stage1~10 공용 프리팹/컨트롤러 참조 검증. 씬 파일은 수정하지 않았습니다.
- `playmode-result.json`: Stage1 PlayMode 조작 53항목 통과. 긴 설명 스크롤은 임시 긴 텍스트를 넣어 확인한 뒤 원래 설명으로 복원해 캡처했습니다. 최종 캡처 텍스트 넘침 0, 새 콘솔 오류 0.
- 기존 TowerManager 부모 연결 오류 8회는 existingErrors에 따로 기록했습니다. 부모가 Tower.prefab 에셋의 Transform을 가리키는 문제이며 이번 UI 작업에서 수정하지 않았습니다.
- 전체 스테이지 플레이/클리어와 배포 빌드는 수행하지 않았습니다.

## 실제 PlayMode 화면

- [타워 능력치](tower-stats.png) · [스킬 설명](tower-skills.png)
- [서포트](tower-support.png) · [시너지](tower-synergy.png)
- [타일 목록](tile-list.png) · [타일 상세](tile-detail.png)
- [적 목록](enemy-list.png) · [적 능력치](enemy-stats.png) · [상태](enemy-effects.png) · [보스](enemy-boss.png)

## 원상 복구

Unity를 닫고 `file-verification.json`의 Files.Path에 해당하는 파일을 `ArtBackups/InfoPanels_20261117`에서 프로젝트의 같은 상대 경로로 덮어쓰면 됩니다. 이후 사용자가 수정한 같은 파일도 덮어쓰므로 먼저 따로 보관하세요. 백업은 원본 8파일과 각각의 .meta, 총 16개이며 원본 SHA-256도 JSON에 있습니다. 새 에디터 도구는 메뉴 실행 전에는 레이아웃을 변경하지 않습니다.

다시 적용: `Tools > SquareTD > 정보 패널 레이아웃 적용` (현재 자식 레이아웃을 재생성하며 실행마다 백업).
다시 확인: `Tools > SquareTD > 정보 패널 플레이 검증` (밸런스 시뮬레이션이 꺼져 있을 때 실행. 실제 씬은 저장하지 않음).