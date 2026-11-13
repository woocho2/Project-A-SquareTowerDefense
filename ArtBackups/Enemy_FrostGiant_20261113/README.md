# 확정 서리 거인 반영

사용자가 확정한 시안을 `Assets/4. Asset/4. Enemy/TwoTone/Jotunheim_FrostGiant.png`에 반영했습니다.

- 확정 생성 원본: `ArtPreviews/Enemy_Jotunheim_20261113/FrostGiant/Jotunheim_FrostGiant_Preview_v1.png` (1254×1254).
- 실제 파일: 2048×2048 투명 PNG, PPU 2048. 원본 그림을 균등 확대했으며 새로 그리거나 선명화하지 않았습니다. 상단에 있는 눈을 중심으로 Face를 설정할 수 있도록 사방 8px 투명 여유를 두었습니다.
- `Jotunheim_FrostGiant_Full`: 전체 2048×2048.
- `Jotunheim_FrostGiant_Face`: 256×256, 캐릭터 왼쪽 눈 위치를 중심으로 설정. 좌표는 `file-verification.json`에 있습니다.
- 기존 GUID `ccc6e19b31db4f93ac394db2babfafd9`, Full ID `4600000000000121`, Face ID `4600000000000122`와 슬라이스 이름을 유지했습니다. 메타는 두 슬라이스 영역만 변경했습니다.
- `FrostGiant_Appearance.asset`은 기존 Face와 Full을 그대로 가리키므로 수정하지 않았습니다. 적 큐브와 UI 연결은 기존 ID로 이어집니다.

수정 전 PNG·메타와 Appearance 에셋·메타는 이 폴더 `Assets/` 아래 같은 상대 경로에 보관했습니다. `manifest.json`은 수정 전 SHA-256입니다. 복원할 때는 같은 경로로 복사하면 되며, 이후 수정한 내용도 덮어쓰므로 확인하세요.

파일 크기·투명 모서리·슬라이스 2개·메타 변경 범위·기존 연결을 확인했습니다. `face-slice-preview.png`는 별도 검수용 얼굴 크롭입니다. **Unity 실제 임포트·플레이 확인은 수행하지 않았습니다.** 게임 코드는 수정하지 않았으며 커밋하지 않았습니다.
