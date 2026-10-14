# 실제 스폰 타일 장식을 사용한 티어 비교 시안

사용자의 정정에 따라 새 L자 장식을 재생성하지 않고, 현재 `Tile_Path_Overlay_Asgard_Atlas.png`의 `Tile_Path_Overlay_Asgard_FRAME` 조각을 직접 추출했다. Unity 슬라이스 좌표는 x512, y0, 256×256이다.

`Source_Spawn_Decoration256.png`이 원본 장식이다. 이 원본의 크기·좌표·알파를 보존하여 배경 → 정면 캐릭터 → 원본 장식 순으로 합성했다. 기존 모서리 금속판과 얇은 연결선까지 그대로 유지했다. 5티어 모두 같은 장식 형태·두께·겹수이며, 티어 색상과 옅은 배경색만 달라진다.

- 최종 검토 이미지: `Tower_Tier_Actual_Spawn_Decoration_Concept_v6.png`
- `*_Preview256.png`: 개별 합성 검토 이미지
- `*_Decoration256.png`: 동일 배치의 티어 색상 장식
- `*_Background256.png`: 티어별 배경
- `Character_Layer256.png`: 공통 캐릭터 레이어
- `verification.txt`: 모든 장식 알파가 원본과 일치하고 GOLD는 전체 RGBA가 원본과 일치함을 확인한 결과
- `Compose-Preview.ps1`: 원본 장식 색상 변경 및 동일 좌표 합성 과정

캐릭터의 배경 분리는 내장 imagegen으로 진행했고, 프롬프트는 `prompt.txt`에 저장했다. 장식은 실제 에셋 픽셀을 사용한 System.Drawing 합성이며 새로 그리지 않았다. 디자인 검토용 시안으로 Assets와 게임 코드는 변경하지 않았고 Unity는 실행하지 않았다.
