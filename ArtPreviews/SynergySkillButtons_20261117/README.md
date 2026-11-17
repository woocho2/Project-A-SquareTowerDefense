# 시너지 타워 스킬 버튼

실제 연결:
- `Assets/2. Prefab/1. Tower.prefab`: SynergySkillButton과 뷰 프리팹 참조를 추가. 기존 .meta/GUID 유지.
- `Assets/2. Prefab/SynergySkillButton.prefab`: 타워 아래의 월드 공간 버튼. RectTransform의 위치·크기, 색, 폰트는 이 프리팹에서 조절 가능.
- 일반 타워에서는 UI를 생성하지 않고 바인딩 컴포넌트도 비활성화. 시뮬레이션에서도 생성하지 않음.
- Stage1의 SynergyBoard 아랫부분만 0.35 월드 단위 늘려 아래쪽 버튼을 수용. 실제 시너지 슬롯 좌표·개수는 유지.

동작:
- 스택 비율을 충전 바와 퍼센트로 표시. 다 차면 금색 버튼 활성화.
- 기존 SynergyBoard.TryUse와 연결. 사용 전/후의 스택 소모·횟수는 기존 스킬 로직이 처리.
- 로키: 버튼 → 첫 번째 길 → 서로 다른 두 번째 길. 뇨르드: 버튼 → 길 한 칸.
- 선택 도중 버튼 재클릭·길 밖 클릭·플레이어 턴 종료로 취소. 취소 시 스택 미소모.
- 로키·뇨르드는 플레이어 턴만. 다른 시너지는 플레이어/적 턴에 사용 가능. 일시정지·대기·게임 종료 중에는 비활성.
- 발드르는 저장된 횟수를 표시하며 스택 비율이 0이어도 보유 횟수가 있으면 활성화.
- 준비 표시·클릭 반응은 DOTween. UI 레이캐스터로 보드 입력과 분리.

구현 파일:
- `Assets/3. Scripts/11. UI/SynergySkillButton.cs`
- `Assets/3. Scripts/11. UI/SynergySkillButtonView.cs`
- `Assets/3. Scripts/12. Input/PickSynergyTargetMode.cs`
- `Assets/Editor/SynergySkillButtonSetup.cs`: 프리팹 연결 메뉴.
- `Assets/Editor/SynergySkillButtonVerification.cs`: Stage1에서 EditMode 테스트와 짧은 PlayMode 검증을 재실행하는 메뉴.
- `Assets/Editor/Tests/SynergySkillButtonTest.cs`: UI/입력 회귀 테스트 10개.
- `Assets/Editor/SupportBoardLayoutTool.cs`: 판 맞추기 메뉴에서도 새 버튼 아래 여유 공간 유지.

확인:
- 전체 C# 컴파일 오류·경고 0개.
- 새 UI 테스트 10개와 기존 시너지 테스트 36개, 총 46개 통과. `editmode-results.xml`, `editmode-summary.txt`.
- 실제 Stage1 PlayMode에서 6기 표시, 충전 전 사용 차단, 준비 후 버튼 이벤트 발동, 월드 UI Raycast, 로키 두 칸 선택 및 소모, 일반 타워 UI 미생성 확인. `playmode-result.json`.
- `charging.png`와 `ready.png`는 실제 PlayMode Game View 캡처. 준비 화면은 검증 중 스택을 채운 상태이며 저장된 기본 충전량을 변경한 것이 아님.
- 테스트가 끝난 뒤 플레이 모드를 종료. 테스트 중 충전·스킬 사용·생성은 씬/데이터에 저장하지 않음.
- 기존 시너지 효과·수치·티어 합·스택 계산·자동 사용 로직은 수정하지 않음. 14종 전체 효과의 실제 전투 연출을 각각 실측한 것은 아님.

수정 전 프리팹·씬 및 .meta: `ArtBackups/SynergySkillButtons_20261117`.
커밋하지 않았습니다.
