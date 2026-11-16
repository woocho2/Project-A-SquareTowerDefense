# 서리 늑대 확정본 적용 전 백업

- 승인된 시안: ArtPreviews/Enemy_Niflheim_20261116/FrostWolf/Niflheim_FrostWolf_Preview_v1.png
- 적용 파일: Assets/4. Asset/4. Enemy/TwoTone/Niflheim_FrostWolf.png 및 .meta
- 1254×1254 생성 원본을 비율 유지하여 2032×2032로 확대하고 사방 8px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 그림 재생성·샤프닝은 하지 않았습니다.
- PPU 2048·Bilinear·Multiple과 나머지 임포트 설정을 유지했습니다. Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자를 중심으로 256×256입니다.
- GUID와 스프라이트 이름·ID를 유지했고 메타 변경은 슬라이스 영역뿐입니다. FrostWolf_Appearance의 큐브 Face·UI Full 참조는 변경하지 않았습니다.
- 기존 PNG·메타와 Appearance 에셋·메타 4개를 원래 상대 경로로 보관했습니다. 백업 SHA-256 검증을 완료했습니다.
- file-verification.json에 파일 검증 결과, face-slice-preview.png에 Face 영역을 저장했습니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원하려면 Unity를 닫고 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로로 덮어쓰세요. 이후 해당 파일에 추가한 변경도 덮어쓰므로 먼저 따로 보관하세요.
