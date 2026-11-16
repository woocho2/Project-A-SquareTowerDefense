# 적 큐브 외형 변경 전 백업

작업 전 원본 12개와 .meta 12개를 원래 상대 경로로 보관했습니다. manifest.json은 실제 백업 파일의 SHA-256입니다. 기존 .meta는 변경하지 않았으며 백업과 일치함을 검증했습니다.

복원할 때 Unity를 닫고 필요한 파일을 같은 상대 경로로 덮어씁니다. 코드·기획서에는 병행 작업이나 이후 수정이 있을 수 있으므로 전체 복원 전에 비교하세요.
이 백업은 아트 작업 직전 기준이며, 앞서 구현한 보스 기믹 자체는 포함된 상태입니다.

이번에 새로 만든 자산은 아래와 같아 이 백업에 원본이 없습니다:
- Assets/4. Asset/4. Enemy/Enemy_Boss_Gimmick_Badge.png 및 .meta
- Assets/4. Asset/0. Common/Enemy_State_BossGimmick.mat 및 .meta
- Assets/Editor/EnemyCubeVisualSetup.cs 및 .meta

전체 되돌림에는 위 신규 파일의 사용 여부를 확인한 뒤 제거하고, 외형 변경을 기록한 20·22·25번 문서도 맞춰야 합니다.
