# 소환마다 큐브의 면과 시작 자세 랜덤 배정

후속 변경: [도착 후 정방향 점프](../enemy-upright-hop/README.md)를 추가했습니다. 이동 중에는 그림도 자연스럽게 구르고, 마지막 도착 뒤에 누운 그림을 큐브 전체의 90도 점프 회전으로 바로 세웁니다.

실제 `Assets/2. Prefab/3. Enemy/Enemy.prefab`에 6개 후보 재질을 지정했습니다. 신규 소환과 풀 재소환 모두 `EnemyMoveController.InitMovement()`에서 `EnemyCubeMeshVisual.RandomizeForSpawn()`을 호출합니다.

| 물리 면 인덱스 | 고정된 면 |
| --- | --- |
| 0 | 앞 Front |
| 1 | 뒤 Back |
| 2 | 왼쪽 Left |
| 3 | 오른쪽 Right |
| 4 | 위 Top |
| 5 | 아래 Bottom |

| 에셋 인덱스 | 후보 |
| --- | --- |
| 0 | 자가치유 SelfHeal |
| 1 | 방어 Defense |
| 2 | 행동 Action |
| 3 | 이동 Mobility |
| 4 | 자물쇠 Locked |
| 5 | 모래시계 Hourglass |

6개 물리 면이 각각 6개 후보 중 하나를 같은 확률로 독립 추첨합니다. 중복을 허용하므로 어떤 종류는 여러 면에 나오거나 아예 나오지 않을 수 있습니다. 면에 배정된 에셋은 굴러가는 동안 유지되고 재소환할 때 다시 뽑습니다. 시작 면은 0~5에서 무작위로 고르되, 처음 보이는 그림은 항상 머리가 위·몸이 아래인 정방향(반시계 회전 0)으로 놓습니다. 정방향 보정은 소환 순간에만 하며, 이후에는 그림도 큐브와 함께 구르므로 옆으로 눕거나 뒤집힐 수 있습니다. 공유 재질과 텍스처 자체를 변경하거나 인스턴스 재질을 생성하지 않습니다.

조회 API:

- `EnemyCubeMeshVisual.GetFaceAssetIndex(int faceIndex)`: 물리 면에 배정된 에셋 번호.
- `EnemyCubeMeshVisual.GetFaceAsset(EnemyCubeFace face)`: 같은 결과의 `EnemyCubeFaceAsset` 열거형.
- `EnemyMoveController.VisibleCubeFaceIndex`: 현재 보이는 물리 면 번호.
- `EnemyMoveController.VisibleCubeAssetIndex`: 현재 보이는 면에 배정된 에셋 번호. 기존 스프라이트 적은 -1.
- `EnemyCubeOrientation.FromFace(face, quarterTurns)`: 지정한 면·회전의 유효한 큐브 자세.

검증: 전체 게임 C# 컴파일과 설치된 프리팹 참조 검사 통과. Unity 6000.3.9f1 별도 PlayMode에서 24개 자세/96개 실제 메시 회전 전이, 480회 실제 풀 소환과 재사용, 6개 시작 면의 정방향과 실제 메시의 머리 방향, 면별 모든 후보의 분포, 논리 인덱스와 실제 재질/메시 자세의 일치, 중복 허용, 공유 프리팹 재질 보존, 코너·역방향 이동 시 배정 유지와 자연스러운 그림 회전, 기존 3×3 정원·예약·빈자리·풀 반환 회귀 검사를 통과했습니다. `random-cubes-on-tile.png`는 Unity 실제 렌더입니다.

면별 능력 효과는 아직 전투에 적용하지 않습니다. 이번 변경은 에셋 배정·시작 자세·인덱스 조회까지입니다. 전체 Stage1 장시간 플레이는 별도로 수행하지 않았습니다. 검증 스크립트는 outputs와 별도 검증 프로젝트에만 둡니다.
