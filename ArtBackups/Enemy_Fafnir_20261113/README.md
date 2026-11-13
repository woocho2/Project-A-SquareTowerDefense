# 파프니르 승인본 적용 전 백업

승인된 `ArtPreviews/Enemy_Nidavellir_20261113/Fafnir/Nidavellir_Fafnir_Preview_v1.png`를 기존 파프니르 PNG에 반영했습니다.

- 생성 원본은 1254×1254입니다. 비율을 유지해 2032×2032로 확대하고, 사방 8px 투명 여백을 둔 2048×2048 PNG로 저장했습니다. 새로 그리거나 샤프닝하지 않았습니다.
- PPU 2048과 기존 임포트 설정을 유지했습니다.
- Full은 전체 2048×2048, Face는 캐릭터 왼쪽 눈동자를 중심으로 256×256입니다.
- PNG GUID, 스프라이트 이름·ID, Appearance의 큐브 Face·UI Full 참조를 유지했습니다. 메타 변경은 두 슬라이스의 영역뿐입니다.
- 기존 PNG·메타, Appearance 에셋·메타 총 4개를 원래 상대 경로로 백업했습니다. manifest.json의 SHA-256과 일치하는지 확인했습니다.
- 파일 검증 결과는 file-verification.json, Face 영역은 face-slice-preview.png에 있습니다. Unity 실제 임포트·플레이는 확인하지 않았습니다.

## 복원

Unity 플레이 모드를 종료하고 에디터를 닫은 다음, 이 폴더의 `Assets` 아래 백업 파일을 프로젝트의 같은 상대 경로에 덮어쓰면 됩니다. 이후 해당 파일에 추가한 변경도 덮어쓰므로 먼저 보관하세요.
