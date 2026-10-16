# 적 큐브 레이어 분리

발키리·타락한 브륀힐드·타락한 로키의 배경·캐릭터·장식·상태 배지를 셰이더에서 합성하도록 연결했다. 새 에셋은 `Assets/4. Asset/4. Enemy/`에 있다. 기존에 분리된 제작 원본을 그대로 내보냈으며 캐릭터를 다시 그리지 않았다.

## 교체 방법

적 프리팹의 `CubeVisual3D` → `EnemyCubeMeshVisual` → **Appearance**에 해당 캐릭터의 설정 에셋을 넣는다. 이 설정 하나가 여섯 면의 캐릭터 이미지와 장비 색 마스크를 함께 바꾼다. 기본 설정은 다음과 같다.

- `Enemy_Valkyrie_Appearance.asset`
- `Enemy_Brunhild_Appearance.asset`
- `Enemy_Loki_Appearance.asset`

새 적은 Project의 Create → SquareTowerDefense → Enemy Cube Appearance에서 설정을 만들고, 투명한 512×512 캐릭터와 같은 좌표의 장비 마스크를 연결하면 된다. 마스크는 흰 부분에만 상태 색을 적용하며 검은 부분은 얼굴·눈·머리·금속의 원래 색을 유지한다. 마스크가 없으면 장비 색은 바뀌지 않는다. 실행 중 교체는 `SetAppearance(profile)`로 한다. 인스펙터에서 설정 에셋을 바꾸면 다음 생성/Awake 때 적용된다.

배경·테두리·상태 배지는 기존 여섯 재질의 설정을 사용한다. `Background Color`로 배경색을 정하고, `State Color (Equipment Mask + Decoration)` 하나로 장비 마스크와 테두리에 같은 상태색을 적용한다. 세 캐릭터 모두 같은 상태면 동일한 색상 값을 사용한다. 별도 Decoration Color는 제거했다.

## 파일과 합성 순서

- `Enemy_Background.png`: 512×512 흰색 바탕 및 둥근 외곽 알파. 재질 색을 곱한다.
- `Enemy_<Character>_Character.png`: 캐릭터별 한 장, 총 3장.
- `Enemy_<Character>_EquipmentMask.png`: 캐릭터별 장비 마스크, 총 3장. Linear 샘플링.
- `Enemy_Decoration.png`: 512×512 흰색·회색 공용 테두리 한 장. 원본 모양·알파·명암을 유지하며, 테두리의 알파를 마스크로 사용해 공통 State Color를 곱한다.
- `Enemy_State_Badges_Atlas.png`: 384×256, 3×2, 배지당 128×128. 순서는 회복·방어·행동 / 이동·자물쇠·모래시계. 각 배지는 512px 면의 위쪽 중앙 128px 영역에 표시한다.

9개 텍스처로 3종 × 6상태를 표시한다. 캐릭터는 MaterialPropertyBlock으로 여섯 재질에 함께 적용하고, 재질의 상태별 배경/장식/문양 설정은 유지한다. `EnemyCubeUnlit.shader`는 배경 → 캐릭터 → 테두리 → 배지를 알파 합성하고 공용 둥근 외곽 마스크를 적용한 뒤 기존 큐브 면의 명암을 계산한다. Linear 프로젝트에서는 장비/테두리 색상 계산을 sRGB에서 수행하고 다시 변환한다. 합성 모드를 끄면 기존 한 장 텍스처 방식도 지원한다.

기존 18개 재질의 GUID와 프리팹의 6면 재질 풀, 그림자, 굴림·낙하·이동 설정은 유지했다. 일반 적·보스 전용 및 기본 보스 프리팹 5개에는 Appearance 참조만 추가했다. 게임플레이 코드·소환 풀·웨이브는 바꾸지 않았다.

## 확인과 기존 이미지

`preview.png`는 CPU로 레이어를 합성한 18상태 미리보기다. 기존 캐릭터의 위치·알파와 장비 마스크 밖 픽셀이 상태마다 유지되는지 검사했다. `verification.txt`에 재질/프로필/프리팹 참조 확인을 기록했다. 변경 C# 4개는 설치된 Unity CoreModule을 참조하는 독립 .NET 빌드에서 경고·오류 없이 컴파일했다. Unity는 실행하지 않았으므로 셰이더 컴파일, 실제 색공간 및 굴림 중 렌더링 확인은 사용자가 수행한다.

후속 정리 요청에 따라 기존 합성 이미지 8개를 삭제하고 에셋 32개를 `Assets/4. Asset/4. Enemy/` 바로 아래에 모았다. 변경 전 전체 파일은 `outputs/enemy-folder-cleanup/before.zip`, 삭제 목록은 같은 폴더의 `deleted-images.txt`에 있다. `before/`에는 변경 전 셰이더·재질·프리팹이 있다. 이후 재질을 재생성할 때는 옛 보스 제작 스크립트 대신 이 폴더의 `Build-Layers.ps1`, `Connect-Layers.ps1`을 사용한다.
