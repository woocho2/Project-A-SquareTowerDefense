# 대기석·시너지 받침판

- `Assets/1. Scenes/3. Stage1.unity`의 기존 SeatBoard와 SynergyBoard를 Unity 에디터 API로 수정·저장했습니다.
- 기존 BoardFrame의 9개 타일과 BoardFrameWhiteNeon 재질을 재활용했습니다. 새 PNG·셰이더·게임 로직은 만들지 않았습니다.
- SeatBoard: 아래 대기석 12칸을 감싸는 가로 판. 보이는 범위 x=-9.1~9.1, y=-6.9~-5.6.
- SynergyBoard: 왼쪽 시너지 6칸을 감싸는 세로 판. 보이는 범위 x=-12~-9, y=-2.5~3.5.
- 원래 Wating Seat·Synergy의 타일, 변환, 좌표를 보존했습니다. 슬롯과 같은 정렬 레이어에서 판은 Order 1, 슬롯은 Order 3으로 뒤에 표시합니다.
- 판에 복제되어 있던 SpecialTileGlowEffect·ZoneGlowEffect만 제거했습니다. 실제 전투 보드·슬롯의 컴포넌트는 그대로입니다.
- `Assets/Editor/SupportBoardLayoutTool.cs`: 다시 맞추는 메뉴 도구. Tools/SquareTD/대기석·시너지 받침판 맞추기. 자동 실행이나 게임 런타임 컴포넌트가 아닙니다.

`before.png`·`after.png`는 Unity의 별도 미리보기 씬에서 실제 씬·카메라·타일·UI를 1920×1080으로 렌더한 결과입니다. 플레이 화면 녹화가 아닙니다.
UI 버튼 배치와 경험치 시스템 코드는 이번 작업 범위에서 변경하지 않았습니다.

검증: native 에디터 적용·씬 저장, 실제 슬롯 12·6칸 보존, 각 판 9조각, 렌더 순서, 씬 .meta 일치를 확인했습니다. `status.json`, `verification.json` 참고.
에디터 도구는 기존 런타임 어셈블리를 참조한 컴파일을 통과했습니다. 이후 전체 프로젝트 컴파일은 병행 중인 경험치·컬러 강화 API와 관련 테스트 변경 때문에 완료하지 못했습니다. 이 로직은 수정하지 않았습니다.
최초 미리보기 복제 시 Global Light 중복 진단이 발생해, 도구의 캡처를 원본 광원을 잠시 비활성화하고 복제 광원만 사용한 뒤 복원하도록 수정했습니다. 실제 씬 광원 설정은 변경하지 않았습니다.
실제 PlayMode 확인은 수행하지 않았습니다.

작업 전 씬·.meta 백업: `ArtBackups/SupportBoards_20261117_102927/Assets/1. Scenes/3. Stage1.unity`.
추가 사용자 변경이 있으면 전체 씬을 바로 덮어쓰지 말고 두 받침판만 비교해 복원하세요.

## 패스 칸 아래의 보드 바탕 (사용자 정정 반영)

최종 결과는 `path-background.png`입니다. `black-path.png`는 길 타일 그림을 잘못 검게 만든 중간 결과이며 채택하지 않았습니다.
길 타일 그림과 스테이지 PathColor는 원래 남색으로 복원했습니다.
검게 바꾼 대상은 메인 BoardTile의 하얀 바탕 중 패스와 겹치는 32칸뿐입니다. 기존 BoardFrameWhiteNeon 셰이더의 타일별 vertex color를 사용하므로 새 셰이더·마스크 이미지는 필요 없습니다.
TileFlags.LockColor를 해당 칸에서만 해제하고 검정을 저장했습니다. 나머지 보드 칸의 색, 길·보드의 배치, 스폰 칸·금색 테두리·화살표·받침판을 보존했습니다.
검증: `path-background.json`. Unity 에디터 실제 렌더·씬 저장·32칸 검정 및 나머지 색 보존 확인. 실제 PlayMode는 별도 미확인입니다.
정정 적용 전 백업: `ArtBackups/PathBackground_20261117_105138`. 최초 길 색 변경 전의 정상 색·스테이지 백업: `ArtBackups/BlackPath_20261117_103944`.
