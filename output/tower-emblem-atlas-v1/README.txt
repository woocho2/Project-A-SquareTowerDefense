타워 문양 흰색 아틀라스 v1

- Tower_Emblem_White_Atlas_1024.png: 1024×1024, RGBA, 4×4 배열.
- 각 칸은 256×256. 왼쪽 위부터 행 순서로 Emblem ID 1~13. 마지막 3칸은 완전 투명.
- 문양은 흰색 #FFFFFF이며 배경은 투명. 흰 타워 배경/티어 프레임은 포함하지 않음.
- individual 폴더에 같은 픽셀의 개별 PNG 13개도 보관.
- Tower_Emblem_Atlas_Preview.png는 확인용 배경/문자/프레임이 포함된 이미지. 실제 아틀라스가 아님.
- Unity_Slices.json에 Unity 좌하단 원점 기준 슬라이스 좌표와 권장 임포트 설정을 기록.
- JSON은 좌표표입니다. Assets 사본에는 .meta와 13개 슬라이스를 생성했고 TowerBase.prefab에 연결했습니다.

배치
1행: 검 / 활 / 방패 / 창
2행: 도끼 / 망치 / 불 / 얼음
3행: 번개 / 바람 / 대지 / 빛
4행: 어둠 / 빈칸 / 빈칸 / 빈칸

Unity 사용
Sprite (2D and UI), Multiple, PPU 256, Pivot Center.
Assets의 적용 사본은 이미 13개 슬라이스가 설정되어 있으므로 다시 자를 필요가 없습니다.
배열 순서는 Emblem ID와 동일하게 유지. 기존 .meta/GUID는 수정하지 않았음.
SpriteRenderer.color로 문양에만 색을 적용. 흰 배경과 티어는 별도 Renderer로 유지해야 같이 물들지 않음.
TowerVisual은 흰 배경을 고정하고 문양에만 공격방식의 색을 적용합니다. TowerInfoPanel도 같은 색을 사용합니다.

제작 및 검증
확정된 활~망치 및 불~어둠의 12개 PNG를 재생성/변형 없이 복사해 포장.
검은 이전에 승인한 수직 대칭 시안을 built-in image_gen으로 투명 실루엣 추출/재생성.
검 source를 균일 리사이즈하고 알파는 유지한 채 RGB만 흰색으로 저장. 픽셀 완전 동일한 AI 추출은 보장하지 않음.
아틀라스 13칸이 개별 PNG와 ARGB 픽셀 단위로 같은지 검증, 빈 3칸 투명도 검증.
기존 PPU와 TowerBase 티어 스케일 기준 13문양×5티어에서 불투명 문양/프레임 겹침 0픽셀.
Assets에 새 아틀라스와 흰 배경을 추가하고 TowerVisual.cs, TowerInfoPanel.cs, TowerBase.prefab을 수정했습니다.
기존 개별 이미지와 기존 .meta/GUID, 티어 위치 및 스케일은 유지했습니다. Integration_Status.txt 참조.
