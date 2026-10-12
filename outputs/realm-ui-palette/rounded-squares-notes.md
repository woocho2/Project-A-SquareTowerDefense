# 공통 UI 아틀라스의 둥근 사각형

Shared_UI_Atlas.png의 정사각형 20개(A/B/C/Frame × 1024, 512, 256, 128, 128_B)를 둥근 사각형으로 바꿨습니다. 기존 모서리 장식과 팔각형 컷을 제거하고, 직사각형 조각과 비슷한 16% 모서리 반경을 적용합니다. 배경의 약한 명암과 프레임의 이중 선 프로필을 사용합니다. 기존 2:1·3:1 등 직사각형 40개와 아틀라스 여백은 픽셀 단위로 동일합니다.

Sliced용은 A/B/C/Frame 각각의 Btn_128_B 네 조각(fileID 2005, 2105, 2205, 2305)입니다. 크기는 모두 128×128, border는 사방 48px입니다. 나머지 56개는 border 0입니다. 사용자가 UI를 직접 맞추기로 했으므로 두 패널의 연결은 이전 상태로 돌렸습니다. GUID, 내부 Sprite ID, rect, pivot은 그대로입니다. UIStageThemeController는 일반 Image의 표시 방식을 이 border에 맞춰 Simple/Sliced로 적용하며, 채움 기능이 있는 Filled 게이지는 유지합니다.

Round-SharedUISquares.ps1에서 결정적인 도형 계산으로 수정했습니다. 예전 아틀라스나 UI 연결을 다시 정리하는 작업은 재개하지 않았습니다. 실제 게임 Canvas의 이전 이미지 참조 확인은 사용자가 맡기로 한 상태입니다.

rounded-squares-preview.png: 수정된 정사각형 20개 비교.
rounded-squares-validation.txt: 픽셀 보존 및 메타데이터 확인.
shared-ui-validation.log: 별도 Unity 프로젝트의 스프라이트 import, 표시 방식, 수동 색상 저장·재열기 검사.
