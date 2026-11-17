# 정보 패널 페이드·전신 비율

- 회전 없이 약 0.2초 페이드. 내용 전환은 페이지에만 적용하고 제목·캐릭터 그림을 유지합니다.
- Full 일러스트/그림자와 적 목록 Face 이미지를 Simple + Preserve Aspect로 설정했습니다. 기존 Sliced 설정이 비율 유지 옵션을 무시하던 문제를 수정했습니다.
- 캐릭터 PNG·슬라이스는 수정하지 않았습니다. 공용 UICanvas.prefab의 해당 이미지 설정만 변경했습니다.
- `scene-bindings.txt`: 10개 스테이지의 공용 패널 연결 확인.
- `playmode-result.json`: Stage1 실제 PlayMode 검증 78항목 통과. 전환 중 페이드/회전 없음/그림 유지, 실제 전신 메쉬 비율, 패널 버튼과 판매 등을 확인했습니다. 텍스트 넘침·새 오류 0. 기존 TowerFactory 부모 참조 오류 8회는 existingErrors에 구분했습니다.
- 화면: [타워](tower-stats.png), [설명](tower-skills.png), [적](enemy-stats.png), [보스](enemy-boss.png), [타일](tile-detail.png).
- 원본 백업: `ArtBackups/InfoPanelTransitions_20261117`. `file-verification.json`에 백업 상대 경로와 해시가 있습니다. Unity를 닫고 해당 파일들을 같은 상대 경로로 덮어쓰면 이번 변경 전으로 돌아갑니다. 이후 수정한 같은 파일도 덮어쓰므로 먼저 보관하세요.

실제 플레이 검증은 Stage1만 수행했습니다. 전체 스테이지 클리어/배포 빌드는 수행하지 않았습니다.