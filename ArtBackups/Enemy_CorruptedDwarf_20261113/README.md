# 확정 일반 드워프 반영

사용자가 확정한 `ArtPreviews/Enemy_Nidavellir_20261113/CorruptedDwarf/Nidavellir_CorruptedDwarf_Preview_v1.png`를 기존 `Assets/4. Asset/4. Enemy/TwoTone/Nidavellir_CorruptedDwarf.png`에 반영했습니다.

- 확정 생성 원본은 1254×1254입니다. 새로 그리거나 선명화하지 않고 균등 확대해 사방 8px 투명 여유가 있는 2048×2048 PNG로 저장했습니다.
- PPU 2048, Full은 이미지 전체 2048×2048, Face는 256×256으로 캐릭터 왼쪽 눈 위치에 맞췄습니다.
- 기존 GUID `d94cd13930c044cf87cfde523910ac5c`, Full ID `4600000000000151`, Face ID `4600000000000152`와 슬라이스 이름을 유지했습니다. 메타는 두 슬라이스 영역만 변경했습니다.
- `CorruptedDwarf_Appearance.asset`은 기존 Face·Full을 올바르게 가리켜 수정하지 않았습니다. 큐브·UI 참조는 기존 ID로 이어집니다.

수정 전 PNG·메타, Appearance 에셋·메타는 이 폴더 `Assets/` 아래 같은 상대 경로로 보관했습니다. 원본 해시는 `manifest.json`, 파일 크기·투명 모서리·슬라이스·연결 검증 결과는 `file-verification.json`에 있습니다. `face-slice-preview.png`는 별도 검수용 얼굴 크롭입니다.

파일과 Face 미리보기는 확인했습니다. **Unity 실제 임포트·Play 모드 확인은 수행하지 않았습니다.** 게임 코드는 수정하지 않았으며 커밋하지 않았습니다.

원복하려면 이 폴더 `Assets/` 안의 파일들을 프로젝트의 같은 상대 경로로 복사하세요. 이후 추가 수정도 덮어쓰므로 복원 전 확인하세요.
