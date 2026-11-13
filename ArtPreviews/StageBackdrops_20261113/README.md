# 스테이지 배경 9종

기존 아스가르드 `Asgard_SkyBackdrop_v2_TopDown_1920x1080.png`를 그림체·탑다운 구도·가장자리 장식·중앙 여유 공간의 기준으로 삼아 새 배경 9종을 만들었습니다. 각 월드의 소재와 색은 20번 기획서의 월드 테마를 참고했습니다. 25번 최신 문서를 읽고 배경 일러스트에 해당하는 규칙을 적용했습니다.

## 저장 위치

모든 실제 PNG는 `Assets/4. Asset/1. BackGround/`에 있습니다.

| 스테이지 | 파일 이름 |
| --- | --- |
| 02 알프헤임 | Alfheim_Backdrop_TopDown_1920x1080.png |
| 03 바나헤임 | Vanaheim_Backdrop_TopDown_1920x1080.png |
| 04 미드가르드 | Midgard_Backdrop_TopDown_1920x1080.png |
| 05 요툰헤임 | Jotunheim_Backdrop_TopDown_1920x1080.png |
| 06 니다벨리르 | Nidavellir_Backdrop_TopDown_1920x1080.png |
| 07 니플헤임 | Niflheim_Backdrop_TopDown_1920x1080.png |
| 08 무스펠헤임 | Muspelheim_Backdrop_TopDown_1920x1080.png |
| 09 헬 | Hel_Backdrop_TopDown_1920x1080.png |
| 10 라그나로크 | Ragnarok_Backdrop_TopDown_1920x1080.png |

## 제작 방식과 검증

- 내장 `image_gen` 도구를 이미지당 한 번씩 사용했습니다. 최종 공통 지시·개별 월드 지시·생성 원본 경로는 `jobs-and-prompts.json`에 있습니다. 원본 이미지는 도구의 generated_images 폴더에 남아 있습니다.
- 생성 원본은 모두 **1672×941**입니다. 1920×1080을 원화 생성 해상도로 보장한 것이 아닙니다. 기존 아스가르드 파일과 같은 규격에 맞추기 위해 전체 그림을 약 1.15배 확대해 **1920×1080**으로 저장했습니다. 자르기·선명화·새 디테일 보강은 하지 않았습니다.
- 9개 PNG는 16:9, 불투명 RGB이며 각 파일의 크기와 SHA-256을 검증했습니다. 실제 에셋 9장의 합계는 **31,342,904 bytes / 29.89 MiB**입니다.
- 생성 결과와 `Stage_Backdrops_ContactSheet.png`에서 위에서 내려다보는 구도, 중앙을 가리는 큰 물체가 없는지, 글자·캐릭터·게임판이 그려져 있지 않은지, 월드별 소재와 색의 구분을 시각적으로 확인했습니다. 미세 생성 패턴의 완전 제거를 정량 보장하지는 않습니다.
- 기존 아스가르드 PNG·메타의 작업 전후 SHA-256은 동일합니다. 이번 작업은 새 PNG를 추가했으며 기존 파일을 덮어쓰지 않았습니다.
- `.meta`와 임포트 설정을 수동으로 만들지 않았습니다. Unity가 파일을 인식하면 메타를 생성합니다. UI Image에 쓸 때는 Texture Type을 Sprite (2D and UI), Sprite Mode를 Single로 설정할 수 있습니다.
- 씬·스테이지 에셋의 배경 참조 연결과 게임 코드 수정은 진행하지 않았습니다. 아직 실제 게임에 적용된 배경으로 보고하지 않습니다.
- **Unity 임포트·Play 모드·셰이더와 타일/UI를 겹친 실제 화면 검증은 수행하지 않았습니다.**

파일별 생성·저장 해상도, 용량, 해시: `file-verification.json`.
전체 비교 미리보기: `Stage_Backdrops_ContactSheet.png`.
내보내기 기록: `export-backdrops.ps1` (기존 대상 파일이 있으면 덮어쓰지 않고 중단).
