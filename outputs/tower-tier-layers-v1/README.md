# 타워 티어 배경·장식 분리 아틀라스

2026-10-14. 사용자의 요청에 따라 현재 타워 스폰타일과 같은 배경·장식 구성을 타워 전용 에셋으로 제작했다.

## 저장 위치

- `Assets/4. Asset/2. Tower/TierLayers/Tower_Backgrounds_Atlas_v1.png`
- `Assets/4. Asset/2. Tower/TierLayers/Tower_Decorations_Atlas_v1.png`

각 아틀라스는 **768×512, 3열×2행, 셀당 256×256**이다. 위쪽부터 왼쪽→오른쪽 순서는 **브론즈 / 실버 / 골드 / 미스릴 / 다이아 / 시너지**. Unity 슬라이스 6개씩, 중앙 피벗, PPU 192, Full Rect, 압축·밉맵 없음.

배경은 옅은 티어 색상과 작은 수직 명암만 포함한다. 장식은 투명한 중앙과 기존 스폰타일의 모서리 판·얇은 연결선을 포함한다. 시너지는 옅은 루비 배경과 짙은 루비색 장식이다. 티어마다 장식이 늘어나지 않고 같은 형태를 유지한다.

## 정확한 크기와 레이어

실제 현재 `Tile_TowerSpawn_Asgard`와 `Tile_Path_Overlay_Asgard_FRAME`의 알파를 사용했다. 셀 캔버스는 256×256, 눈에 보이는 외곽은 `(7,7)`에서 **242×242**, 중심 `(128,128)`이다. 각 레이어의 알파는 모든 티어에서 동일하며 가로·세로·대각 대칭이다. 배경과 장식의 외곽이 일치한다.

프리팹 합성 순서는 **배경 → 캐릭터 일러스트 → 장식**이다. 세 레이어를 같은 위치·크기에 놓고 장식을 캐릭터 앞에 표시하면 아랫부분과 어깨가 자연스럽게 가려진다. 문양은 사용자가 별도로 배치한다. 이 작업은 기존 캐릭터·타일·프리팹·런타임 코드 연결을 변경하지 않았다. Unity 실행 및 검증은 사용자가 수행한다.

## 제작 기록

내장 imagegen으로 원본 스폰타일을 참조한 색상·명암 이미지를 제작했다. 최종 프롬프트는 `prompt-backgrounds.txt`, `prompt-decorations.txt`; 생성 원본은 `backgrounds-native.png`, `decorations-native.png`이다. 생성 원본에서 장식의 중앙 채움과 배경의 가장자리 선이 생겼으므로, `Build-Atlases.ps1`은 원본 스폰타일 알파로 외곽과 투명 중앙을 정확히 복원했다. 배경은 생성 이미지의 중앙 색상을 수직으로 샘플링하여 테두리·문양 없는 채움만 남겼다. 장식의 색상과 명암은 생성 원본에서 유지했다.

`Save-Assets.ps1`은 최종 PNG와 스프라이트 메타를 저장한다. `asset-manifest.json`과 `sprite-map.csv`에는 이름·슬라이스·GUID·fileID가 있다. `verification.txt`에는 정확한 크기·대칭·분리·메타 확인 결과가 있다. `preview.png`에서 배경, 장식, 일반 티어의 히어로·시너지의 오딘 일러스트 합성을 볼 수 있다.

## 시너지 루비색 적용 (2026-10-14)

사용자가 승인한 루비색 시안을 두 아틀라스의 마지막 시너지 셀에 반영했다. `ruby-approved/native-approved.png`는 내장 imagegen으로 만든 승인 원본이며, 프롬프트는 `ruby-approved/prompt.txt`에 있다. `ruby-approved/Apply-Ruby.ps1`은 승인 시안의 배경·장식 색상을 분리해 기존 알파에 적용하므로 다른 5종의 RGBA와 모든 외곽·장식 마스크·중앙 투명도·메타는 그대로다.

`ruby-approved/preview.png`는 현재 에셋으로 오딘을 합성한 미리보기다. 캐릭터 PNG는 변경하지 않았다. 기존 전체 미리보기의 시너지 칸도 갱신했다. 재제작할 때 `Build-Atlases.ps1`과 `Save-Assets.ps1` 실행 후 마지막에 `ruby-approved/Apply-Ruby.ps1`을 실행한다. 이번 적용의 검증 기록은 `ruby-approved/verification.txt`에 있다.

## 현재 연결 상태 (2026-10-14 후속 요청)

사용자의 후속 요청으로 두 아틀라스를 `TowerBase.prefab`과 `TowerVisual`에 연결했다. 최초 에셋 제작 단계에서 연결하지 않았다는 설명과 구분한다. 일반 타워는 각 티어 셀, 시너지는 마지막 루비 셀을 자동 선택한다. 상세 내용과 기존 에셋 정리 기록은 `outputs/tower-visual-integration-v1/README.md`에 있다.
