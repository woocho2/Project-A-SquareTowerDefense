# 확정 서리 거인 족장 반영

사용자가 확정한 `ArtPreviews/Enemy_Jotunheim_20261113/FrostGiantChief/Jotunheim_FrostGiantChief_Preview_v1.png`를 기존 `Assets/4. Asset/4. Enemy/TwoTone/Jotunheim_FrostGiantChief.png`에 반영했습니다.

- 1254×1254 확정 생성 원본을 균등 확대해 2048×2048 투명 PNG로 저장했습니다. 새로 그리거나 선명화하지 않았습니다. 사방 8px 투명 여유를 두었습니다.
- PPU 2048, Full은 전체 2048×2048, Face는 256×256이며 캐릭터 왼쪽 눈 위치를 중심으로 맞췄습니다.
- 기존 GUID `9e409f1d2c5a47adbcfbb308306dd914`, Full ID `4600000000000131`, Face ID `4600000000000132`와 이름을 유지했습니다. 메타는 두 슬라이스 영역만 변경했습니다.
- 기존 `FrostGiantChief_Appearance.asset`은 Face·Full을 올바르게 가리켜 변경하지 않았습니다. 큐브·UI 연결은 기존 ID로 이어집니다.
- 이 폴더 `Assets/`에 수정 전 PNG·메타, Appearance 에셋·메타를 같은 상대 경로로 보관했습니다. 원본 해시는 `manifest.json`, 파일·슬라이스·연결 검증 결과는 `file-verification.json`에 있습니다.
- `face-slice-preview.png`는 별도 검수 이미지입니다. 실제 게임 에셋이 아닙니다.
- 파일 검증과 Face 미리보기 확인을 수행했으며, Unity 실제 임포트·Play 모드 확인은 수행하지 않았습니다. 게임 코드 수정과 커밋은 하지 않았습니다.

원복은 이 폴더 `Assets/` 안의 파일들을 프로젝트의 같은 상대 경로로 복사하면 됩니다. 이후에 추가한 수정도 덮어쓰므로 복원 전 확인하세요.
