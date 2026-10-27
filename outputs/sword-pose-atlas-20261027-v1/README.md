# 검 타워 포즈 변경 시험 적용

- 날짜: 2026-10-27
- 제작: built-in image_gen. 승인된 네 포즈를 기준으로 셀 간 간섭과 잘린 검 끝을 정리한 뒤 아틀라스 규격으로 배치.
- Sword_Atlas.png: 적용할 1024×1024 RGBA 아틀라스.
- sprites/: 네 개의 512×512 스프라이트 검토본.
- before/Sword_Atlas.png 및 .meta: 교체 직전 원본.
- before/asset-generation-rules.txt: 교체 전 규칙 문서.
- Approved_Pose_Concept.png: 사용자가 선택한 포즈 시안.
- Pose_Separated_Source.png: 배치 정리에 사용한 생성 원본.
- validation.json: 해시, 좌표, 여백 및 참조 검증 기록.

네 역할 순서: 왼쪽 위 검성, 오른쪽 위 검객, 왼쪽 아래 검선, 오른쪽 아래 검귀.
공통 배율로 배치했으며 셀 사방 최소 16px 투명 여백을 검사했습니다. 서로 다른 포즈의 얼굴 좌표가 동일하다고 검증한 것은 아닙니다.
Unity 실제 플레이와 화면상 가독성은 아직 확인하지 않았습니다.
