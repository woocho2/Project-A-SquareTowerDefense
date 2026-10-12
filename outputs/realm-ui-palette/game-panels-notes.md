# GameInfoPanel / PlayerTurnPanel 아틀라스 연결

Canvas.prefab의 두 패널에서 배경·프레임 30개를 공통 Shared_UI_Atlas.png로 연결했습니다. A 9개는 아이보리 #FFFCEC, B 1개는 황금 #ECBB41, C 5개는 밤의 남색 #263855, Frame 15개는 황금 #ECBB41입니다. 원래 아이콘, 텍스트, 버튼 이벤트, 투명 클릭 영역은 유지합니다.

RectTransform의 실제 비율로 조각을 선택합니다. 예: 225×75 → 3:1의 384×128, 225×150 → 3:2의 384×256, 150×225 → 2:3의 256×384. 웨이브 UI는 실제 300×100에 맞춰 3:1로 연결하고 잘못된 3:2 이름도 3:1로 수정했습니다.

사용자 지시에 따라 아틀라스를 늘리지 않았습니다. 턴 종료 그룹·버튼 클릭 영역·배경·프레임을 모두 400×100으로 맞추고 기존 4:1(512×128) C/Frame 조각을 사용합니다. 정사각형 프레임은 기존 단일 128×128 Sliced 조각을 사용하며 나머지 배경·프레임은 Simple입니다.

다른 패널의 이미지, 테마별 이전 참조 목록을 다시 정리하지 않았습니다. 현재 두 패널의 이미지와 관련된 씬의 Sprite/Color/Type 및 턴 종료 높이 override만 같은 값으로 맞춥니다.

확인: 실제 Canvas.prefab을 별도 Unity 프로젝트에 import하여 30개 실제 Sprite GUID/fileID, 비율, 색상, Image mode, 턴 종료 그룹/클릭 영역/배경/프레임의 정렬을 검사했습니다. 아틀라스는 그대로 4096×2048, 60개 스프라이트입니다. 본 게임 Play Mode는 실행하지 않았습니다.

Connect-GamePanels.ps1: 연결 작업 스크립트.
game-panels-assignments.json: UI 경로와 선택한 스프라이트 기록.
GamePanelsCheck.cs / game-panels-validation.log: 실제 Unity import 검증.
