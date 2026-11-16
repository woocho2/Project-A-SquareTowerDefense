# 대장 서리 늑대 적용 전 백업

승인된 ArtPreviews/Enemy_Niflheim_20261116/FrostWolfAlpha/Niflheim_FrostWolfAlpha_Preview_v1.png를 기존 Niflheim_FrostWolfAlpha.png에 적용했습니다.

- 1254×1254 원본을 비율 유지하여 2032×2032로 확대하고 사방 8px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 재생성·샤프닝은 하지 않았습니다.
- PPU 2048과 기존 임포트 설정을 유지했습니다. Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자를 중심으로 256×256입니다.
- PNG GUID와 스프라이트 이름·ID를 유지했습니다. 메타 변경은 슬라이스 영역뿐이며, Appearance의 큐브·UI 연결은 변경하지 않았습니다.
- 기존 PNG·메타와 Appearance 에셋·메타 총 4개를 원래 상대 경로에 백업했습니다. manifest.json의 SHA-256 검증을 완료했습니다.
- 일반 서리 늑대의 기존 승인본도 다시 확인했습니다. 두 캐릭터의 PNG·슬라이스·백업·Appearance 확인 결과는 both-wolves-verification.json입니다.
- Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원하려면 Unity를 닫은 뒤 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로에 덮어쓰세요. 이후 수정도 덮어쓰므로 먼저 보관하세요. 일반 서리 늑대의 별도 원본 백업은 ArtBackups/Enemy_FrostWolf_20261116에 있습니다.
