# 남은 몬스터 에셋 21종

기준: `Assets/20. 통합 기획서.txt`의 9장 스테이지별 몬스터 구성. 기존 아스가르드·알프헤임 6종 다음으로 3~9스테이지의 일반 / 중간 보스 / 최종 보스 각 1종을 제작했다.

| 스테이지 | 폴더 | 일반 | 중간 보스 | 최종 보스 |
|---|---|---|---|---|
| 3 바나헤임 | Vanaheim | LowerSpirit · 타락한 하급 정령 | MiddleSpirit · 타락한 중급 정령 | UpperSpirit · 타락한 상급 정령 |
| 4 미드가르드 | Midgard | SeaSerpent · 바다뱀 | GreatSeaSerpent · 큰 바다뱀 | Jormungandr · 요르문간드 |
| 5 요툰헤임 | Jotunheim | FrostGiant · 서리 거인 | FrostGiantChief · 서리 거인 족장 | Ymir · 이미르 |
| 6 니다벨리르 | Nidavellir | CorruptedDwarf · 타락한 드워프 | CorruptedDwarfArtisan · 타락한 드워프 장인 | Fafnir · 파프니르 |
| 7 니플헤임 | Niflheim | FrostWolf · 서리 늑대 | FrostWolfAlpha · 대장 서리 늑대 | Fenrir · 펜리르 |
| 8 무스펠헤임 | Muspelheim | FireGiant · 불 거인 | FireGiantChief · 불 거인 족장 | Surtr · 수르트 |
| 9 헬 | Hel | Wight · 망자 | Garm · 가름 | HelQueen · 헬 |

라그나로크는 기획대로 1~10웨이브 미드가르드, 11~20웨이브 니플헤임, 21~30웨이브 헬, 31~40웨이브 무스펠헤임의 세 몬스터 구성을 재사용한다. 새 초상이 필요하지 않다.

## 저장 및 사용

각 `Assets/4. Asset/4. Enemy/<폴더>/`에 아래 파일을 저장했다.

- `<ID>_Character.png`: 512×512 RGBA 정면 초상, 투명 배경.
- `<ID>_EquipmentMask.png`: 512×512 RGB 이진 마스크. 장비·의복은 흰색, 얼굴·눈·머리·동물 털·비늘·자연 불꽃 등은 검은색. 털로 만든 망토는 착용 장비에 해당한다.
- `<ID>_Appearance.asset`: 위 두 텍스처를 참조하는 기존 `EnemyCubeAppearance` 형식.
- 각 파일 및 스테이지 폴더의 Unity `.meta`.

기존 `EnemyCubeMeshVisual`의 Appearance에 해당 에셋을 지정하거나 `SetAppearance()`로 넘길 수 있다. 공용 배경·테두리·6상태 배지는 기존 시스템을 사용한다. 이번 작업은 외형 에셋 제작이며, 웨이브/스테이지의 소환 데이터와 프리팹 연결은 변경하지 않았다.

## 공통 크기

일반 타워와 앞서 정리한 몬스터의 동일 기준을 사용한다. 논리 256칸 기준 눈 중앙 `(128,160)`, 눈 사이 거리 `39.5`, 턱 기준선 `184`, 몸 하단 `244`. 실제 512텍스처에는 두 배로 적용한다. 전체 머리장식 높이가 아닌 얼굴 기준으로 정렬하므로 왕관·귀·갈기 같은 종별 실루엣은 유지된다.

초상과 마스크에 같은 좌표 보정을 적용했다. 금색 홍채가 머리/피부색과 겹치는 일반 드워프와 수르트는 `manual-landmarks.json`의 시각 검토한 동공 좌표를 사용했다. 나머지 19종은 최종 이미지의 홍채 픽셀을 측정했다.

## 미리보기와 확인

- `overview.png`: 21종을 실제 공용 타일·배지·상태색과 합성한 전체 목록.
- `<Realm>-six-states.png`: 각 스테이지의 3종 × 6상태 비교.
- `portraits-overview.png`: 최종 투명 초상 크기 비교.
- `equipment-mask-check.png`: 최종 마스크를 자홍색으로 표시한 검토용 이미지.
- `verification.json`: 해상도·알파·마스크 이진성·외형 참조·공통 크기 검사 결과.

21종 모두 512규격과 참조 검사를 통과했다. 최종 몸 하단은 488px로 동일하다. `overview.png` 등 기본 미리보기는 CPU 합성이다. 추가 요청으로 Unity 에디터의 실제 임포트/플레이/렌더링도 아래 범위에서 검증했다.

## Unity 실제 검증 완료

Unity **6000.3.9f1**, Direct3D11 / NVIDIA GeForce RTX 4070 Ti에서 별도 검증 프로젝트를 실행했다. 열려 있는 작업 씬을 수정하지 않고, 실제 프로젝트의 큐브 비주얼 코드·메시·셰이더·공용 재질·팔레트·27종 외형을 복사하여 검사했다. 렌더링은 검증 프로젝트의 기본 렌더 파이프라인에서 진행했으므로 원본 URP 스테이지 전체 플레이 테스트와는 범위가 다르다.

- 기존 6종 + 신규 21종, 외형 27개와 텍스처 54개 실제 임포트 및 참조 확인.
- 모든 면의 초상/마스크 및 상태색 바인딩 각각 2,916회 확인.
- 랜덤 소환 324회: 첫 면이 정방향으로 나오는지 확인.
- 네 방향 굴림 2,700개 시점: 이동 진행도, 유효한 위치/크기, 세 번 굴린 최종 자세 확인.
- 낙하/착지 567개 시점: 유효한 변환과 원위치 착지 확인.
- 27종 × 6상태 = 실제 GPU 렌더링 162장 저장 및 빈 화면/분홍 오류 셰이더 여부 확인.
- 검사 중 수집한 몬스터 실행 오류 0개.
- 원본 게임 스크립트 전체 컴파일: 경고 0개, 오류 0개.

`unity-rendered-overview.png`는 신규 21종 실제 렌더링 목록이다. `unity-rendered/`에는 162장 원본과 스테이지별 여섯 상태 비교 시트가 있다. 실행 결과는 `unity-runtime-verification.json`, 원본과 검증 복사본 일치 결과는 `unity-input-verification.json`, 컴파일 기록은 `compile-verification.json`에 저장했다.

검증 프로젝트 초기 구동 중 Unity Editor Search의 인덱스 초기화 예외가 로그에 있었다. 이 예외는 몬스터 검사 시작 전에 발생했으며, 임포트·플레이 검사는 정상 완료했다. 원본 프로젝트의 콘솔 전체에 오류가 없다는 의미로 해석하면 안 된다.

현재 `WaveManager`의 풀은 기존 아스가르드 프리팹을 사용한다. 신규 몬스터의 스테이지별 소환 연결 및 실제 스테이지 전투 검증은 포함하지 않았다.

## 제작 기록과 재구성

생성 모드: **built-in `image_gen`**. 초상 21회와 장비 마스크 21회를 각각 독립 호출했다. CLI/API 키를 사용하지 않았다.

- `production-plan.json`: 공통 초상 프롬프트 + 종별 디자인/눈색 프롬프트.
- `mask-prompts.json`: 21종별 마스크 프롬프트와 원본 생성 위치.
- `references/`: 기존 시너지 타워의 이미르·수르트·헬 동일 인물 참고 크롭.
- `source/`: 생성 원본. Unity Assets 바깥에 보존.
- `asset-manifest.json`: 안정적인 GUID, 원본/최종 크기 보정 정보.
- `before-final-layout/`: 공통 크기 최종 보정 전 초상·마스크 쌍.

원본으로부터 최종 에셋을 재구성하려면 저장소 루트에서 `node outputs/remaining-monsters-v1/Rebuild-Final.cjs`를 실행한다. 해당 작업은 이 21종의 결과물을 다시 쓰고 기존 GUID를 유지한다. 개별 Build 스크립트만 실행하면 마지막 크기 보정이 빠지므로 전체 스크립트를 사용한다. Sharp는 이 작업 환경의 `_codex_spreadsheet_task/node_modules/sharp`를 참조한다.
