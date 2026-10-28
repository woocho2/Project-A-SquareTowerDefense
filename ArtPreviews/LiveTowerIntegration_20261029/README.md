# 일반 타워 최신 아틀라스 실전 적용 — 2026-10-29

일반 14문양 56종에 최신 승인 이미지를 적용했다. 검·도끼·활 3장은 이미 최신본이었고 나머지 11장을 교체했다.
판 위 캐릭터와 정보 카드가 사용하는 기존 경로를 갱신했으며 시너지 타워는 이번 변경 대상이 아니다.

- 이미지 규격: 아틀라스 1024×1024, 역할별 512×512, PPU 512, Bilinear.
- 기존 14개 메타와 카탈로그·타워 프리팹: Unity 임포트 후에도 바이트 단위 동일.
- Unity 6000.3.9f1: 14장 임포트, 정보 카드 56종·판 위 56종 참조, 5티어 280개 프리팹 이미지 선택 검사 통과.
- 검사 중 경고 0건. 실제 씬 Play 모드와 화면 렌더링 검증은 수행하지 않았다.
- 검사용 임시 Editor 스크립트는 실행 후 Assets 밖 이 폴더로 이동했다.
- 원본 백업: Before/. 적용 전후 해시: Preflight.json. Unity 결과: [Unity_Audit.json](Unity_Audit.json).

| 문양 | 최신 원본 | 실전 경로 |
| --- | --- | --- |
| Sword | ArtPreviews/WeaponCartoon_20261028/Tower_Sword_Atlas_Cartoon.png | Assets/4. Asset/2. Tower/TowerCharacter/Sword_Atlas.png |
| Axe | ArtPreviews/WeaponCartoon_20261028/Tower_Axe_Atlas_Cartoon.png | Assets/4. Asset/2. Tower/TowerCharacter/Axe_atlas.png |
| Bow | ArtPreviews/Bow_20261028/Cartoon_v2/Tower_Bow_Atlas_Concept_v2.png | Assets/4. Asset/2. Tower/TowerCharacter/Bow_Atlas.png |
| Spear | ArtPreviews/Spear_20261028/Cartoon_v2/Tower_Spear_Atlas_Concept_v2.png | Assets/4. Asset/2. Tower/TowerCharacter/Spear_Atlas.png |
| Shield | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Shield_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Shield_Atlas.png |
| Hammer | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Hammer_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Hammer_Atlas.png |
| Fire | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Fire_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Fire_Atlas.png |
| Water | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Water_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Water_Atlas.png |
| Earth | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Earth_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Earth_Atlas.png |
| Wind | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Wind_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Wind_Atlas.png |
| Ice | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Ice_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Ice_Atlas.png |
| Electric | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Electric_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Electric_Atlas.png |
| Light | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Light_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Light_Atlas.png |
| Dark | ArtPreviews/RemainingTowers_20261028/Cartoon_v1/Atlases/Tower_Dark_Atlas_Cartoon_v1.png | Assets/4. Asset/2. Tower/TowerCharacter/Dark_Atlas.png |
2026-10-29 중복 정리: Assets/TowerCharacter의 참조 없는 Cartoon 사본 11장과 메타를 삭제했다. 실전 14문양 + 시너지 1장만 유지한다. 최신 실전 PNG·메타, 두 카탈로그, 타워·UI 프리팹의 변경 없음 확인. 삭제 메타와 기록은 DuplicateCleanup/에 보관한다.

2026-10-29 이름 정리: 검·도끼 이름을 기준으로 다른 13장과 메타를 함께 변경했다. 현재 경로는 Plan.json, 이전 이름 대응과 보존 검증은 Rename_Report.json을 따른다. Preflight.json·Unity_Audit.json 등 이전 시점 기록에는 당시 이름을 유지한다.
