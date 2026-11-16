# 불 거인 확정본 적용 전 백업

승인된 ArtPreviews/Enemy_Muspelheim_20261116/FireGiant/Muspelheim_FireGiant_Preview_v1.png를 기존 Muspelheim_FireGiant.png에 적용했습니다.

- 1254×1254 생성 원본을 비율 유지하여 2016×2016으로 확대하고 사방 16px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 눈이 높은 위치에 있어 Face를 눈 중심으로 배치할 수 있도록 최소 여백을 확보했습니다. 재생성·샤프닝하지 않았습니다.
- PPU 2048과 기존 임포트 설정을 유지했습니다. Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자 중심의 256×256입니다.
- GUID와 스프라이트 이름·ID를 유지했습니다. 메타 변경은 슬라이스 영역뿐이며, FireGiant_Appearance의 큐브 Face·UI Full 참조를 유지했습니다.
- 기존 PNG·메타, Appearance 에셋·메타 4개를 원래 상대 경로로 보관했습니다. manifest.json의 백업 SHA-256 검증을 완료했습니다.
- file-verification.json에 파일 검증 결과와 face-slice-preview.png에 Face 영역을 저장했습니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

복원하려면 Unity를 닫은 다음 이 폴더의 Assets 아래 파일을 프로젝트의 같은 상대 경로에 덮어쓰세요. 이후 해당 파일에 추가한 변경도 덮어쓰므로 먼저 보관하세요.
