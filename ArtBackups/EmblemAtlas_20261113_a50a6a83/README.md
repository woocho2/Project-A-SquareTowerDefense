# 문양 아틀라스 교체 백업

사용자가 승인한 흰색 문양 14종을 기존 `Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png`에 교체했습니다. 프리즘 문양은 중앙 빛줄기를 없애고 겹쳐진 삼각 테두리를 유지했습니다.

## 변경 파일

- `Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png`: 1024×1024, 256×256 셀 16칸 중 기존 위치 14칸 사용. 기존 `.meta`는 바이트 단위로 동일합니다.
- `Assets/1. Scenes/2. Menu.unity`: `Img_Emblem_*` 이미지 중 검을 잘못 가리키던 13곳의 `m_Sprite`만 각 문양으로 수정했습니다. 작업 도중 사용자가 저장한 최신 씬을 기준으로 수정했습니다.

`TowerIcons.asset`의 문양 14개는 이미 올바른 순서로 연결되어 있어 수정하지 않았습니다. 게임 코드와 다른 에셋 연결은 수정하지 않았습니다.

## 검증

- `file-verification.json`: PNG 크기, 14개 셀과 빈 2개 셀, 최소 16px 투명 여백, 보이는 픽셀 RGB 255, 프리즘 중심 투명도 0, 기존 메타 유지, 메뉴 연결, 카탈로그 순서 검증 결과입니다.
- `atlas-preview.png`: 기존 아틀라스 칸 순서의 미리보기입니다.
- `all-emblems-preview.png`: 112px와 36px 크기 미리보기입니다.
- `generated-transparent-source.png`, `imagegen-prompt.txt`: 이미지 생성 원본과 요청입니다. PNG 포장 시 RGB를 흰색으로 정규화하고 거의 보이지 않는 알파 잔여값 1~4는 제거했으며, 내부 알파 249~255는 255로 맞췄습니다. 중간 알파 경계는 유지했습니다.
- Unity 실제 임포트·플레이 검증은 수행하지 않았습니다.

## 원본 보관과 복원

루트 `Assets/`와 `manifest.json`에는 작업 시작 시 원본 6개 파일을 보관했습니다. `before-apply/`에는 작업 중 저장된 메뉴 씬을 포함한 중간 시점 원본을 추가 보관했습니다. 각각의 manifest에 SHA-256이 있습니다.

최종 수정 직전 메뉴 씬은 `file-verification.json`의 `MenuBeforeApplyBackup`에 적힌 `menu-before-final-apply-*.unity`입니다. 메뉴를 정확히 작업 직전으로 복원하려면 이 파일을 `Assets/1. Scenes/2. Menu.unity`로 복사하세요. 작업 이후 저장한 메뉴 수정도 덮어쓰므로 먼저 확인하세요.

아틀라스 원복은 이 폴더의 `Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png`를 같은 프로젝트 경로로 복사하면 됩니다. `.meta`는 변경하지 않았으며 원본도 함께 보관되어 있습니다.

`prepare-atlas.ps1`과 `apply-and-verify.ps1`은 이번 작업의 기록용 도구입니다. 적용 스크립트는 작업 전 해시와 일치할 때만 실행되므로 완료 후 반복 실행하지 마세요. 커밋하지 않았습니다.
