# 수르트 확정본 적용 전 백업

얼굴 타락 무늬가 추가된 승인 시안 ArtPreviews/Enemy_Muspelheim_20261116/Surtr/Muspelheim_Surtr_Preview_v2.png를 기존 Muspelheim_Surtr.png에 적용했습니다.

- 1254×1254 생성 원본을 비율 유지하여 2032×2032로 확대하고 사방 8px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 재생성·샤프닝하지 않았습니다.
- PPU 2048과 기존 임포트 설정을 유지했습니다. Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자 중심의 256×256입니다.
- PNG GUID·스프라이트 이름·ID를 유지했습니다. 메타 변경은 슬라이스 영역뿐이며 Surtr_Appearance의 큐브 Face·UI Full 참조를 유지했습니다.
- 기존 적 PNG·메타와 Appearance 에셋·메타 4개를 원래 상대 경로로 보관했습니다. manifest.json의 백업 SHA-256 검증을 완료했습니다.
- 시너지 타워 원본 00_Synergy_5_Surtr.png와 메타의 SHA-256 불변을 확인했습니다.
- file-verification.json은 파일 검증 결과, face-slice-preview.png는 Face 영역입니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원하려면 Unity를 닫은 뒤 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로에 덮어쓰세요. 이후 수정도 덮어쓰므로 먼저 보관하세요.
