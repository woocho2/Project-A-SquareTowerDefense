# 검객·발키리 새 시안 — 2026-10-17

## 검객 아틀라스 적용

사용자 요청에 따라 기존 `Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png`의 오른쪽 위 검객 칸을 새 그림으로 교체했습니다. 다른 세 초상은 픽셀 단위로 동일하며 기존 `.meta`도 바이트 단위로 유지했습니다. 백업과 검증 결과는 `atlas-apply/`에 있습니다. 발키리 후보는 아직 게임에 연결하지 않았습니다.

## 발키리 전체 크기 보정

최신 발키리 후보는 `Valkyrie_Character_v3.png`입니다. 내장 image_gen으로 전체 캐릭터의 여백을 조정한 뒤 검객의 실제 알파 경계에 맞춰 정규화했습니다. 512×512 캔버스에서 가로 400px(78.125%), 아래 기준선 496px(96.875%)으로 검객과 같습니다. 이전 v2의 가로 493px 대비 약 19% 줄었습니다. 디자인의 종횡비를 유지했고 투명 여백을 넣었습니다. `Swordsman_Valkyrie_Size_Matched.png`는 같은 캔버스 배율에서 비교한 그림입니다. 생성 프롬프트는 `Valkyrie_Size_Prompt.txt`, 수치 검증은 `valkyrie-size-verification.json`입니다. 기존 Unity 참조는 그대로입니다.

## 최초 생성 시안

내장 image_gen으로 기존 초상을 참고하여 각각 새로 생성했습니다. 원본 생성 이미지는 `Swordsman_Generated.png`, `Valkyrie_Generated.png`이며 실제 사용 크기의 투명 PNG도 저장했습니다.

- `Tower_Sword_Swordsman_Front_v3.png`: 256×256. 삿갓과 남색 복장을 유지하고 양쪽 시선·동공·눈꺼풀을 정리한 검객.
- `Tower_Sword_Front_Atlas_v3.png`: 512×512. 기존 검 아틀라스의 오른쪽 위 검객 칸만 새 그림으로 교체한 후보. 다른 세 초상은 원본과 픽셀이 같습니다.
- `Valkyrie_Character_v2.png`: 512×512. 볼·턱과 노출된 얼굴의 비중을 줄이고 어깨와 가슴 갑옷의 비중을 늘린 발키리.
- `Comparison_Before_After.png`: 위 검객 / 아래 발키리, 왼쪽 기존 / 오른쪽 새 버전.
- `prompts.json`: 생성에 사용한 전체 프롬프트 두 개와 도구 방식.
- `verification.json`: 크기·알파 및 다른 아틀라스 칸 보존 확인.

최초 생성 시에는 기존 Unity 에셋·메타데이터·참조를 수정하지 않았으며, 이후 검객 아틀라스 적용은 위에 별도로 기록했습니다. 새 발키리는 장비 위치도 바뀌었으므로 기존 장비 색상 마스크와 바로 짝지어 사용할 수 없으며, 실제 적용할 때 새 실루엣에 맞춘 마스크가 필요합니다.
