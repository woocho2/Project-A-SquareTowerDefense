# 헬 원본 적용 전 백업

사용자가 지정한 기존 시너지 타워 그림 Assets/4. Asset/2. Tower/TowerCharacter/00_Synergy_4_Hel.png를 적 Assets/4. Asset/4. Enemy/TwoTone/Hel_HelQueen.png에 그대로 복사했습니다.

- 원본이 이미 2048×2048이므로 확대·재생성·색 변경 없이 PNG 바이트를 그대로 사용했습니다. 시너지 타워 원본과 메타는 변경하지 않았습니다.
- 기존 적 PPU 2048과 임포트 설정을 유지했습니다. 적 Full은 전체 2048×2048, 적 Face는 캐릭터 왼쪽 눈동자 중심의 256×256입니다. 타워 원본의 슬라이스는 변경하지 않았습니다.
- 기존 적 GUID·스프라이트 이름·ID를 유지했습니다. 메타 변경은 슬라이스 영역뿐이며 Appearance의 큐브·UI 참조를 유지했습니다.
- 교체 전 적 PNG·메타, Appearance 에셋·메타 총 4개를 원래 상대 경로로 보관했습니다. manifest.json의 백업 SHA-256을 검증했습니다.
- tower-source-hashes.json으로 타워 원본 PNG·메타 불변, 적 PNG와 타워 PNG의 SHA-256 일치를 확인했습니다.
- file-verification.json, garm-hel-verification.json은 파일 검증 결과이며 face-slice-preview.png는 Face 영역입니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원: Unity를 닫고 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로에 덮어쓰세요. 이후 수정도 덮어쓰므로 먼저 보관하세요.
