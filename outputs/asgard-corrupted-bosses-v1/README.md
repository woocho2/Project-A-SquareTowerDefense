# 아스가르드 타락한 브륀힐드·로키 6색 에셋

2026-10-16. 현재 시너지 타워의 브륀힐드·로키 정면 초상을 참고하여 검은 갑옷·망토와 차분한 표정의 보스 버전을 제작했다. 브륀힐드의 금발·푸른 눈·날개형 투구·두 갈래 머리, 로키의 검녹색 머리·녹색 눈·뿔관·깃털 어깨를 유지했다.

## 최종 에셋

`Assets/4. Asset/4. Enemy/Asgard/CorruptedBosses_v1/`

- `Enemy_FallenBrunhild_States_Atlas_v1.png`: 중간 보스 브륀힐드 6색
- `Enemy_FallenLoki_States_Atlas_v1.png`: 최종 보스 로키 6색
- `Materials/Enemy_Fallen<Boss>_<State>.mat`: 보스마다 상태별 6개, 총 12개. 기존 `EnemyCubeUnlit` 셰이더와 해당 보스의 아틀라스 한 장을 사용한다. 개별 색상 PNG를 중복 저장하지 않는다.

아틀라스는 각각 **1536×1024**, 3열×2행, 셀당 **512×512**, Sprite Multiple, **384 PPU**, 중앙 피벗, Full Rect, 무압축, 밉맵 없음이다. 캔버스의 월드 크기는 기존 패스 타일과 같은 **1.3333333**이다. 실제 그림 외곽은 모든 면에서 `(14,14), 484×484`로 기존 적과 정확히 같다.

아틀라스 순서는 위 **자가치유(빨강) / 방어(노랑) / 이동력(하늘색)**, 아래 **행동력(초록) / 자물쇠(회색) / 모래시계(보라)**다. 기존 발키리의 배경·장식 레이어와 원형 상태 심볼을 재사용했다. 캐릭터는 정면 그림의 6색 변형이며 옆·뒤·정수리의 여섯 시점이 아니다. 후속 위치 통일 요청으로 각 보스의 자가치유 그림 한 장을 공통 원본으로 고정했다. 여섯 면의 얼굴·눈·턱·머리카락·어깨는 정확히 같은 픽셀과 알파를 사용하고, 장비 포인트 색상과 배경·프레임·상태 배지만 바뀐다.

## 연결 참고

SpriteRenderer에는 아틀라스를 펼쳐 원하는 스프라이트를 연결할 수 있다. 기존 `EnemyCubeMeshVisual`의 재질 풀에 연결할 때는 `cubeAssetPoolIndex` 순서를 따른다: **자가치유 / 방어 / 행동력 / 이동력 / 자물쇠 / 모래시계**. 가운데 두 상태는 아틀라스의 행 순서와 다르므로 주의한다. `asset-manifest.json`과 `sprite-map.csv`에 슬롯, 스프라이트 ID, 재질, UV 좌표를 정리했다. 각 재질은 1/3 × 1/2 UV 영역만 표시하므로 동일 아틀라스를 공유한다.

후속 연결 요청에 따라 `BossCharacters/EnemyBoss_Brunhild.prefab`, `EnemyBoss_Loki.prefab`을 기존 일반 적과 같은 실제 3D 메시 큐브로 전환했다. 기존 기본 프리팹 `4. Enemy_middleBoss.prefab`, `5. Enemy_boss.prefab`에도 같은 구성을 적용했다. 각 보스의 여섯 재질을 MeshRenderer와 EnemyCubeMeshVisual의 후보 풀에 연결했으며, 소환할 때 각 면의 상태를 독립적으로 무작위 배치하는 기존 동작을 사용한다.

`=====Pool=====.prefab`과 `3. Stage1.unity`의 보스 프리팹 참조를 각각 브륀힐드·로키 전용 프리팹으로 변경했다. 루트 배율 1/3, 큐브 한 변 1.3333334, 이동 배율 1, 타일당 굴림 시간 0.4초와 그림자·낙하 설정은 일반 적과 동일하다. 기존 2D 프리팹의 Z 배율 0을 수정하고 Rigidbody2D를 일반 적처럼 Kinematic/회전 고정으로 맞췄다. HP 바는 큐브 앞쪽 Z=-1.5에 배치했다. 체력·방어·행동력 데이터, 보스 종류와 웨이브 규칙은 유지했다.

`Connect-Cubes.ps1`과 `Verify-CubeConnection.ps1`, `cube-connection-verification.txt`에 연결 및 정적 검사를 남겼다. 변경 직전 파일은 `cube-connection-before/`에 보관했다. 로컬 fileID·재질 순서·메시 참조·게임플레이/HP 바 참조 보존과 풀/씬의 두 참조 변경만 발생했음을 검사했다. Unity 실행·플레이 검증은 하지 않았다.

## 제작 기록

내장 imagegen으로 두 보스와 각 6색을 제작했다. 초기 프롬프트는 `prompts/Brunhild.txt`, `prompts/Loki.txt`; 생성 원본은 `source/`에 있다. 위치 통일 편집은 내장 imagegen으로 장비 색상 선택 마스크를 만들고, 원본의 실제 붉은 장비 픽셀로 마스크를 보정한 뒤 공통 초상의 장비만 색상 변환한다. 얼굴이나 실루엣을 다시 생성하지 않는다. 해당 프롬프트는 `aligned-portraits/mask-prompt.txt`에 있다.

`Build-Bosses.ps1`은 `aligned-portraits/Align-Portraits.ps1`을 호출하여 공통 초상과 고정 프레임·배경·원형 심볼로 재생성한다. 필요한 제작 레이어는 정리 압축본에서 `aligned-portraits/templates/`로 추출했으며 Assets 밖에서만 보관한다. `Save-Assets.ps1`은 PNG와 스프라이트 메타, 아틀라스 UV를 쓰는 3D 큐브 재질을 저장한다. 변경 전 아틀라스와 제작 스크립트는 `aligned-portraits/before/`에 있다.

`overview.png`에서 12종을 비교할 수 있다. `verification.txt`는 외곽·대칭·원형 심볼 원본 보존, 최종 PNG SHA256·슬라이스·재질 참조 검사를 기록한다. Unity 렌더링·임포트 검증은 사용자가 수행한다.
