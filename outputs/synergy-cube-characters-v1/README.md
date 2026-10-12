# 정사각형 시너지 캐릭터와 굴림 보스

2026-10-12 기획의 최신 시너지 13종을 기존 발키리와 같은 카툰 SD 그림체로 제작했습니다. 사용자의 추가 지시를 반영하여 얼굴·머리·투구를 합친 외곽을 정사각형으로 통일했습니다. 정사각형 카드 뒤에 얼굴을 얹는 구조가 아니라 캐릭터 머리 자체가 네모입니다. 그림체는 매끈한 카툰이며 픽셀 아트나 입체 큐브를 그리지 않았습니다.

## 시너지 타워

`Assets/4. Asset/2. Tower/SynergyCube/`의 `Synergy_*_Front_v2.png`가 최종 정적 정면 에셋입니다. 브륀힐드, 프레이, 프레이야, 토르, 이미르, 브록 & 에이트리, 스카디, 울르, 수르트, 로키, 헬, 발드르, 오딘을 제작했습니다. 브록 & 에이트리는 한 칸에 두 형제가 함께 들어갑니다.

`Assets/Resources/SynergyCharacterCatalog.asset`에서 논리 ID 6001~6013과 정면 그림을 연결합니다. `TowerBase.prefab`의 `TowerVisual`에 연결되어 있으므로 시너지 소환 시 새 그림을 사용합니다. 이전 문양·흰 배경·티어 장식 레이어를 숨기고, 정면 캐릭터만 표시합니다. 타일 바닥과 맵 장식은 TileManager가 표시합니다. 타워의 캐릭터 캔버스는 스폰 타일과 같은 256px / 192 PPU = 1.333333 월드 단위이며, 실제 머리 그림은 그 안의 224px 정사각형입니다. 움직이거나 굴리는 컴포넌트를 타워에 추가하지 않았습니다. 일반 타워를 다시 적용하면 기존 문양 크기와 레이어를 복원합니다.

TowerManager의 이전 명단(헤임달·비다르·트루드·발키리) 및 바뀐 문양 조합을 최신 27번 표로 맞췄습니다. 기존 6001~6013 ID는 유지합니다. 효과는 기존처럼 생성·표시 전용이고 새 공격·버프 능력을 만들지 않았습니다.

## 적과 보스

`Assets/4. Asset/4. Enemy/BossCube/`의 `Boss_*_Faces_v2.png`는 브륀힐드·로키·이미르·수르트·헬의 3×2 아틀라스입니다. 칸은 256px, 전체는 768×512px입니다. 상단은 Front/Left/Right, 하단은 Back/Top/Bottom입니다. 각 면을 Unity Sprite Multiple로 잘랐고 fileID 1001~1006을 프리팹에 연결했습니다.

`Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_*.prefab`에 다섯 보스를 준비했습니다. 아스가르드에서 현재 사용하는 `4. Enemy_middleBoss.prefab`과 `5. Enemy_boss.prefab`도 각각 타락한 브륀힐드와 타락한 로키로 연결했습니다. 이미르·수르트·헬은 해당 스테이지의 적 풀에 넣을 수 있는 개별 프리팹을 준비했습니다. 다른 스테이지의 웨이브·풀을 새로 구현하지 않았습니다. 기획대로 이미르·수르트에는 "타락한"을 붙이지 않습니다.

일반 발키리도 `Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/Enemy_Cube_FallenValkyrie_Faces_v4.png`의 정사각형 여섯 면으로 통일했습니다. 기존 세 일반 적 프리팹과 이를 상속하는 행동력·전체회복 외형 변형이 사용합니다. 검은 장비와 보석만 다섯 색으로 변경하는 기능은 유지했습니다. 기존 장비 셰이더에는 Square Head Layout 및 실제 오른쪽 면 좌표를 추가했습니다.

EnemyCubeVisual에 선택적인 `m_right`를 추가했습니다. 오른쪽 그림이 있으면 그대로 표시하고, 없으면 기존처럼 왼쪽 그림을 반전합니다. 면은 각 Sprite 안의 (16,16,224,224) 영역을 사용합니다. 정수 큐브 자세, 연속 굴림, Move Speed, 착지 대기 및 충돌체·HP 바 분리 동작은 유지했습니다.

## 생성과 확인

내장 `image_gen` 도구로 제작했습니다. 최종 프롬프트는 `prompts.json`, 생성 원본은 `source/`, 이미지 경로 및 GUID는 `manifest.json`, 개별 생성 기록은 `square-front-records/`와 `square-boss-records/`에 있습니다. 초기 시안 및 이전 버전은 사용자 요청에 따라 삭제했습니다.

`Import-Characters.ps1`은 생성 원본에서 완전히 채워진 머리 영역을 잘라 224×224px 정사각형으로 맞추고 256px 투명 캔버스에 넣습니다. 이 과정에서 외곽의 작은 머리카락·투구 돌출과 불필요한 투명 여백을 제외하며 새 바탕색을 그리지 않습니다. 아틀라스도 각 면에 같은 처리를 적용합니다. 발키리 v1~v3, 이전 네 면 파일 및 시너지 v1은 최신 연결 확인 후 삭제했습니다.

`browser-preview.png`와 `unity-preview.png`는 최종 둥근 모서리 렌더링입니다. 이전 각진 비교 이미지는 삭제했습니다. `preview.html`은 시너지 갤러리와 보스 선택·굴림·이동 속도 조절을 제공합니다. 기존 `../cube-enemy-roll/preview.html`도 새 일반 발키리 여섯 면과 색상으로 갱신했습니다.

검증: 캐릭터 연결 변경까지의 Runtime/Editor 컴파일, 실제 기하 소스의 24자세 및 연속성 검사, 13개 정적 타워의 스폰 타일 크기와 일반 타워 복원, 5개 보스 프리팹의 6개 실제 Sprite 참조 및 오른쪽 면 선택, Unity GPU 렌더링, 브라우저 갤러리·굴림, 일반 발키리 30개 면/색상 조합의 얼굴·금색·알파 보존을 확인했습니다. `UnityCharacterCheck.cs`, `unity-validation.log`, `validation.txt`를 참고하세요. Unity 검사는 별도 임시 프로젝트에서 수행했으며 본 게임의 Play Mode 실행은 하지 않았습니다.

## 모서리 조정

머리 외곽 224px의 모서리 반경을 8px로 살짝 둥글게 했습니다. 원본 PNG는 유지하며 셰이더에서 모서리 알파를 자릅니다. 공통 CharacterCornerMask.cginc를 일반 적 장비 셰이더와 RoundedCharacterSprite.shader가 함께 사용합니다. 타워는 TowerVisual의 Synergy Corner Radius, 적과 보스는 EnemyCubeVisual의 Corner Radius에서 조절합니다. 일반 타워로 복원할 때에는 기존 머티리얼과 PropertyBlock도 복원합니다. 이번 모서리 변경의 C# 및 셰이더 GPU 검사는 별도 Unity 프로젝트에서 통과했습니다.

최종 전체 게임 Runtime/Editor C# 컴파일과 캐릭터 모듈의 독립 Unity 컴파일·렌더링을 통과했습니다. 전체 게임 Play Mode는 실행하지 않았습니다.

## 이전 버전 정리

사용자 요청으로 사용하지 않는 이미지 12개, 보스 장비 머티리얼 2개와 각 .meta, 이전 발키리 시안 폴더 4개, 옛 미리보기·프리팹 스냅샷을 삭제했습니다. 현재 남긴 게임 에셋은 시너지 정면 v2 13개, 보스 여섯 면 v2 5개, 일반 발키리 여섯 면 v4 1개입니다. 일반 적의 다섯 장비 팔레트와 둥근 모서리 셰이더·머티리얼은 유지합니다. 삭제 전후 Assets/Packages/ProjectSettings의 GUID 참조를 검사했습니다. 삭제 목록은 cleanup-report.txt에 있습니다. 이미지 가져오기 스크립트는 남아 있는 시너지 v2 메타를 템플릿으로 사용하며 이전 보스 팔레트를 다시 만들지 않습니다.
