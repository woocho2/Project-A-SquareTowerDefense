# 스테이지별 배경 연결

Stage2~10 씬의 `BackGround` SpriteRenderer에 각 월드 배경을 연결했습니다.

| 씬 | 배경 |
| --- | --- |
| 4. Stage2 | Alfheim |
| 5. Stage3 | Vanaheim |
| 6. Stage4 | Midgard |
| 7. Stage5 | Jotunheim |
| 8. Stage6 | Nidavellir |
| 9. Stage7 | Niflheim |
| 10. Stage8 | Muspelheim |
| 11. Stage9 | Hel |
| 12. Stage10 | Ragnarok |

씬마다 `BackGround`의 `m_Sprite` 한 줄만 수정했습니다. 배경 9개의 PNG.meta는 `spritePixelsToUnits`를 100에서 기존 아스가르드와 같은 72로 바꿨습니다. 모두 1920×1080이므로 월드 크기가 26.6667×15로 같아집니다. 위치·스케일·머티리얼·레이어·색·씬의 다른 설정은 변경하지 않았습니다.

기존 PNG 픽셀, 아스가르드 Stage1, 각 스프라이트 GUID·슬라이스 이름·ID·영역, 씬 메타는 유지했습니다. 게임 코드는 수정하지 않았습니다.

## 백업과 검증

- 이 폴더의 `Assets/`에는 수정 전 씬 9개와 씬 메타 9개, 배경 PNG 메타 9개를 원래 상대 경로로 보관했습니다. PNG는 수정하지 않았으므로 복제하지 않고 해시를 기록했습니다.
- `manifest.json`: 백업 파일·해시, 변경하지 않은 PNG·Stage1 등의 해시.
- `file-verification.json`: 스테이지별 배경 GUID·fileID·PPU·연결 검증 결과.
- `minimal-diff-verification.json`: 씬별 이미지 참조 한 줄, 메타별 PPU 한 줄 외에 변경이 없는지 확인한 결과.
- Unity가 이미 생성한 실제 `.meta`의 GUID·sprite ID를 읽어서 연결했습니다. 실제 Unity Play 모드의 화면 검증은 수행하지 않았습니다.

복원하려면 Unity에서 관련 씬을 닫은 상태에서 이 폴더 `Assets/` 안의 파일들을 프로젝트의 같은 상대 경로로 복사하세요. 그 이후의 씬·임포트 변경도 덮어쓰므로 복원 전 확인하세요. 전체 git checkout이나 reset은 필요 없습니다.

`connect-and-verify.ps1`은 이번 작업의 실행 기록이며 재실행용이 아닙니다. 커밋하지 않았습니다.
