# 타락한 헬 확정본 적용 전 백업

사용자가 승인한 ArtPreviews/Enemy_Hel_20261116/HelQueen/Hel_HelQueen_Corrupted_Preview_v1.png를 기존 Assets/4. Asset/4. Enemy/TwoTone/Hel_HelQueen.png에 적용했습니다. 앞선 원본 그대로 적용을 이번 타락 버전으로 교체했습니다.

- 1254×1254 생성 원본을 비율 유지하여 2032×2032로 확대하고 사방 8px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 재생성·샤프닝하지 않았습니다.
- PPU 2048과 기존 임포트 설정을 유지했습니다. Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자 중심의 256×256입니다.
- 기존 적 GUID·스프라이트 이름·ID와 Appearance의 큐브 Face·UI Full 참조를 유지했습니다. 메타 변경은 Face 슬라이스 위치뿐입니다.
- 교체 직전 적 PNG·메타와 Appearance 에셋·메타 4개를 원래 상대 경로로 보관했고 manifest.json의 백업 SHA-256을 검증했습니다.
- 시너지 타워 원본 PNG·메타는 변경하지 않았으며 이전 tower-source-hashes.json과 일치함을 확인했습니다.
- file-verification.json은 파일 검증 결과, face-slice-preview.png는 Face 영역입니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원: Unity를 닫고 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로에 덮어쓰세요. 이후 수정도 덮어쓰므로 먼저 보관하세요. 이 백업은 타락 그림 적용 직전의 원본 복사 상태로 돌아갑니다. 그보다 이전의 적 그림은 ArtBackups/Enemy_HelQueen_20261116에 보관되어 있습니다.
