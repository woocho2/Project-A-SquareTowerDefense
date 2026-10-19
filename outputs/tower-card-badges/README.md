# Tower card badges

기존 Tower_Card_Icon_Atlas_v1의 그림체에 맞춘 교체용 UI 배지 24종.
제작 방식은 **built-in image_gen**이며, 최종 생성 프롬프트는 [prompts.json](prompts.json)에 기록했다.
최종 선택 원본은 `source/`, 입력 목록은 `sources-final.json`, 전체 미리보기는 [preview.png](preview.png)다.

## Unity 에셋

저장 폴더: `Assets/4. Asset/5. UI/`

| 아틀라스 | 구성 | 해상도 / 배열 |
| --- | --- | --- |
| Tower_Card_Attack_Atlas.png | Splash, Single, Buff, Debuff | 1024×1024 / 2×2 |
| Tower_Card_Tier_Atlas.png | Bronze, Silver, Gold, Mithril, Diamond, Synergy | 1536×1024 / 3×2 |
| Tower_Card_Emblem_Atlas.png | 기획서 문양 01~14 | 2048×2048 / 4×4 |

각 PNG의 `.meta`에 Multiple 슬라이스를 설정했다. 위에서 아래, 왼쪽에서 오른쪽 순서이며 남는 셀은 투명하다.
모든 스프라이트는 **512×512 / PPU 100 / 중심 피벗 (0.5, 0.5) / Border 0 / Full Rect**.
그림의 최대 변은 448px, 기본 여백은 32px다. 공격방식과 티어는 가로세로 448px의 동일한 외곽 규격이며, 문양은 원래 종횡비를 유지한다.
UI Image의 RectTransform을 고정하고 Source Image를 교체하면 된다. 원색 그림을 사용하므로 Image의 Color는 흰색을 기준으로 한다.

### 공격방식

1. `CardBadge_AttackSplash`: 빨강, 광역 폭발
2. `CardBadge_AttackSingle`: 파랑, 단일 표적
3. `CardBadge_AttackBuff`: 아이보리~노랑, 강화 십자
4. `CardBadge_AttackDebuff`: 검정~보라, 약화 화살표

### 티어

`CardBadge_TierBronze`, `CardBadge_TierSilver`, `CardBadge_TierGold`, `CardBadge_TierMithril`, `CardBadge_TierDiamond`, `CardBadge_TierSynergy`.
같은 다이아 실루엣에 구리색 / 은색 / 금색 / 민트색 / 밝은 청백색 / 루비색을 사용했다.
시너지는 오른쪽 아래 여섯 번째 셀(512×512, 중앙 피벗, PPU 100)이다. 기존 다섯 티어와 atlas GUID/fileID를 유지했다.
시너지 그림도 **built-in image_gen**으로 제작했으며 프롬프트는 [synergy-prompt.txt](synergy-prompt.txt)에 기록했다.

### 문양

접두사 `CardBadge_Emblem` 뒤에 아래 이름이 붙는다. 기획서 번호 순서와 같다.

| 번호 | 이름 | 문양 |
| --- | --- | --- |
| 01 | Sword | 검 |
| 02 | Axe | 도끼 |
| 03 | Bow | 활 |
| 04 | Spear | 창 |
| 05 | Shield | 방패 |
| 06 | Hammer | 해머 |
| 07 | Fire | 불 |
| 08 | Water | 물방울 |
| 09 | Earth | 대지 |
| 10 | Wind | 바람 |
| 11 | Ice | 얼음 |
| 12 | Electricity | 전기 |
| 13 | Light | 빛 |
| 14 | Dark | 어둠 |

## 보존 및 확인

기존 `Tower_Card_Icon_Atlas_v1.png`와 `.meta`는 변경하지 않았다. 따라서 CardStat 6종과 기존 참조의 GUID/fileID도 유지된다.
새 세 아틀라스는 별도 GUID를 사용하며, 기존 검·광역·브론즈 그림은 원본에서 가져와 새 규격에 배치했다.
현재 패널의 코드와 프리팹 연결은 기존 상태를 유지했다. 이번 산출물은 요청한 교체용 아틀라스다.

`verification.json`에 해시, 슬라이스 좌표, 실제 그림 범위와 이름을 기록했다. 셀 크기·피벗·PPU·GUID 중복·기존 파일 해시를 파일 수준에서 확인했다. Unity 실행 검증은 수행하지 않았다.

`Pack.ps1`은 생성 원본을 셀로 추출하고, 인접 셀에서 넘어온 작은 고립 조각을 제외한 뒤 규격을 통일해 패킹한다. 게임용 PNG는 Assets에만 3장 저장한다.
재패킹 시에는 `Pack.ps1 -Rebuild`를 사용한다. 기존 에셋이 마지막 기록 이후 수정되었으면 덮어쓰지 않고 중단한다.
