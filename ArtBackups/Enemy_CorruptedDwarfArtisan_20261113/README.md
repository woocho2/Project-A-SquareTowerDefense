# 확정 드워프 장인 반영

사용자가 확정한 `ArtPreviews/Enemy_Nidavellir_20261113/CorruptedDwarfArtisan/Nidavellir_CorruptedDwarfArtisan_Preview_v1.png`를 기존 `Assets/4. Asset/4. Enemy/TwoTone/Nidavellir_CorruptedDwarfArtisan.png`에 반영했습니다.

- 확정 생성 원본은 1254×1254입니다. 균등 확대하여 사방 8px 투명 여유가 있는 2048×2048 PNG로 저장했습니다. 새로 그리거나 선명화하지 않았습니다.
- PPU 2048, Full 전체 2048×2048, Face 256×256으로 캐릭터 왼쪽 눈 위치를 중심으로 설정했습니다.
- 기존 GUID `ba4480353c6148f49730660a210bd167`, Full ID `4600000000000161`, Face ID `4600000000000162`와 슬라이스 이름을 유지했습니다. 메타는 두 슬라이스 영역만 변경했습니다.
- `CorruptedDwarfArtisan_Appearance.asset`은 기존 Face·Full을 가리키므로 수정하지 않았습니다. 큐브·UI 연결은 기존 ID로 유지됩니다.
- 이 폴더 `Assets/`에 수정 전 PNG·메타, Appearance 에셋·메타를 같은 상대 경로로 보관했습니다. `manifest.json`은 원본 해시, `file-verification.json`은 파일·슬라이스·연결 검증 결과입니다. `face-slice-preview.png`는 별도 검수 이미지입니다.

파일 검증과 Face 미리보기 확인을 수행했습니다. **Unity 실제 임포트·Play 모드 확인은 수행하지 않았습니다.** 게임 코드는 수정하지 않았고 커밋하지 않았습니다.

원복하려면 이 폴더 `Assets/` 안의 파일들을 프로젝트의 같은 상대 경로로 복사하세요. 이후 수정도 덮어쓰므로 복원 전 확인하세요.
