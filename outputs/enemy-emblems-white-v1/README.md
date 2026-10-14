# 몬스터 유형용 흰색 심볼 7종

기존 타워의 단색 문양처럼 굵고 단순한 실루엣으로 제작했다. 흰색(255,255,255)과 투명 배경을 사용한다. SVG 원본을 직접 작성하고 PNG로 출력했으며 이미지 생성 모델은 사용하지 않았다.

| 유형 | 심볼 | 키 |
|---|---|---|
| 자가치유형 | 회전 화살표 + 십자 | SelfHeal |
| 이동력형 | 발 + 속도선 | Mobility |
| 행동력형 | 번개 | Action |
| 방어형 | 방패 | Defense |
| 전체회복형 | 동료 셋 + 십자 | GroupHeal |
| 중간보스형 | 왕관 | MiddleBoss |
| 최종보스형 | 왕관을 쓴 해골 | FinalBoss |

사용자가 예시로 든 유형 외에 프로젝트의 `equipment-palettes.json`에 있는 방어형·전체회복형을 포함했다. `EnemyType` 열거형은 아직 Normal/Speed/Depend/MiddleBoss/Boss이며 행동력형·전체회복형은 시각 변형으로 존재한다. 이번 작업에서는 분류 코드와 프리팹을 변경하지 않았다.

## 파일

- 게임 에셋: `Assets/4. Asset/4. Enemy/Emblems_v1/`.
- 개별 `Enemy_Emblem_<Key>_White_v1.png`: 256×256, RGBA, Sprite Single, 256PPU, 중앙 피벗, 무압축, 밉맵 없음, FullRect.
- `Enemy_Emblems_White_Atlas_v1.png`: 1024×512, 4열×2행, 256px 셀 7개. 위 자가치유·이동력·행동력·방어, 아래 전체회복·중간보스·최종보스·빈칸. 7개 슬라이스의 메타데이터 포함.
- `svg/`: 수정·확대 가능한 흰색 SVG 원본. `png/`: 개별 PNG 복사본.
- `overview.png`: 라벨이 있는 전체 시안.
- `preview.html`: 흰색과 유형 색상, 배경 변경 및 16/24/32/48/64px 크기 비교.
- `manifest.json`: 유형·파일·GUID·아틀라스 슬라이스 ID 대응표.

기본 이미지는 모두 흰색이다. Unity에서 `SpriteRenderer.color`나 UI Image의 Color로 빨강·하늘색·초록 등 유형별 색상을 적용할 수 있다. 기존 발키리 시안의 빨간 회복 아이콘을 대체할 때는 SelfHeal에 빨강을 곱하면 된다.

`Build-Emblems.cjs`로 재현할 수 있다. 일곱 개 시안을 시각적으로 확인했고 크기·흰색 픽셀·투명 테두리를 검사했다. 이 결과물은 이미지 에셋 제작까지이며 실제 몬스터 프리팹 연결은 수행하지 않았다. Unity 편집기를 실행해 임포트한 검사는 수행하지 않았다.
