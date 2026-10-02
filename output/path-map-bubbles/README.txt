패스 맵 버프 기포 효과

적용: Assets/1. Scenes/3. Stage1.unity의 TileManager에 연결됨.
프리팹: Assets/2. Prefab/4. Effect/PathMapBuff_Bubbles.prefab
이미지: Assets/4. Asset/1. BackGround/VFX/VFX_Path_Bubble_White.png
스크립트: Assets/3. Scripts/06. Effect/PathTileBubbleEffect.cs

소환타일: 기존 위성 효과 유지.
패스타일: 맵 버프가 있는 칸만 최대 기포 2개 표시. 반 주기 간격으로 올라옴.
크기: 첫 적용보다 40% 확대. 금색 원 안쪽을 유지하도록 가로 분산 범위 축소.
색: 방어 노랑 / 이동 하늘색 / 회복 초록. 흰색 원본에 색상 적용.
기포 주기: 게임 시간 기준 2.8초. 배속에 따라 빨라지고 일시정지 시 멈춤.
위치: 타일 중앙의 금색 원 안쪽. 방향표시와 적보다 아래에 렌더링.
버프 계산과 게임 난수는 변경하지 않음. 연출용 난수는 별도로 사용.
반복 생성 시 기존 TileManager의 이펙트 정리 기능 사용.

미리보기 왼쪽부터: 일반 / 방어 / 이동 / 회복.
Path_Map_Bubbles_Comparison.png: 확대 비교.
Path_Map_Bubbles_64px.png: 64px 타일 크기 비교.
Path_Map_Bubbles_Animated.gif: 움직임 합성 미리보기. Unity 실플레이 캡처는 아님.

검증: 현재 런타임 스크립트 전체를 포함한 MSBuild 컴파일 오류 0, 경고 0.
Unity 플레이 모드의 실제 렌더링은 별도 확인 필요.
이미지 생성: built-in image_gen 사용. 최종 프롬프트는 ImageGen_Prompt.txt.
