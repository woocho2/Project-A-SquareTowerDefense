제작 완료: 아스가르드 원본을 유지하고 나머지 9개 스테이지용 아틀라스 신규 추가.

저장 위치: Assets/4. Asset/1. BackGround/Tile_Path_Overlay_{Realm}_Atlas.png
테마: Vanaheim, Alfheim, Jotunheim, Midgard, Nidavellir, Niflheim, Hel, Muspelheim, Ragnarok.
모든 아틀라스: 1024x768 RGBA, 256x256 스프라이트 12개, PPU256, Bilinear, 압축 없음.
배치: 상단 N/NE/E/SE, 중단 S/SW/W/NW, 하단 START/END/JUMP/RETURN.
글씨는 원본 아이보리 유지. 박스 귀퉁이 장식 없음. 박스 크기 226x80 동일.

방식: built-in image_gen 편집으로 스테이지별 팔레트 제작, 승인된 아스가르드 마스터에 활성/비활성 색상만 적용.
AI가 바꾼 모양과 글자는 복사하지 않음. Build_Realm_Overlays.ps1에 동일 규격 색상 적용 절차 보관.
프롬프트: AI_Edit_Prompts.txt
AI 원본: 스테이지별 AI_Palette_Source.png
전체 테마 비교: All_Realms_Comparison.png (정적 합성 미리보기이며 Unity 플레이 화면은 아님)
각 테마 12개 비교: {Realm}/12Sprites_Composite.png

검증: 모든 테마 alpha 변경 0, 원본 아이보리 글자 보호 픽셀 13717개 변경 0.
활성 방향 원본 마스크 픽셀 개수 동일: 1210,1238,1225,1362,1333,1305,1287,1285.
모든 .meta 12개 슬라이스/이름/PPU/고유 GUID와 PNG 복사 일치 확인.
기존 아스가르드 PNG/.meta, 패스타일 아틀라스, Stage1 씬, Tiles.prefab 해시 변경 없음.
게임 코드, 기존 타일 에셋, 기존 씬 연결 변경 없음. 새 테마 자동 선택/전환 로직은 이번 작업에 추가하지 않음.
Unity Editor 플레이 테스트는 수행하지 않음.

새 아틀라스 GUID:
Vanaheim: 6b7340ad33a84685b966f9eb7e27a04e
Alfheim: 87460d3c72ef4aa3bcba98a02bfab72b
Jotunheim: 477d96c4374a43aebd8464f619f845db
Midgard: 110684a952e04657ac8317ac7be15d34
Nidavellir: a7250e92e7ee40128680d966fafd6af6
Niflheim: 29e6d5880def4ed29acef6bcdbf05c19
Hel: 29f4c23080e345dcba849239059ba3d9
Muspelheim: 65bff3f11ba746fbb119387546d22ebd
Ragnarok: f6a33e498efe4f4cb43db28e80dd354a

