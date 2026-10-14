# 새 타워 외형 연결 및 기존 에셋 정리

2026-10-14. 일반 14문양 × 4공격방식의 새 정면 일러스트 56종, 시너지 14종, 티어 배경·장식 각 6종을 연결했다.

- 일반 카탈로그: `Assets/Resources/TowerCharacterCatalog.asset`. 검·도끼도 다른 12문양과 같은 카탈로그 경로를 사용한다.
- 시너지 카탈로그: `Assets/Resources/SynergyCharacterCatalog.asset`. 정적 정면 일러스트 14종을 새 아틀라스로 연결했다.
- 공통 프리팹: `Assets/2. Prefab/2. Tower/TowerBase.prefab`.
- 외형 코드: `Assets/3. Scripts/01. Tower/TowerVisual.cs`.

프리팹의 표시는 **Background(2) → Character(3) → Decoration(4) → Emblem(5)** 순서다. 일반 타워는 기존 95% 크기, 시너지는 기존 스폰타일 크기를 유지한다. 배경·초상·장식은 같은 캔버스 크기·위치로 표시한다. 일반 문양은 프리팹의 위쪽에 별도 배치하여 공격방식 색만 곱한다. 위치와 크기는 `Emblem` 자식에서 조절할 수 있다. 시너지는 6번째 루비 배경·장식을 사용한다. 초상 전체를 색칠하지 않는다.

정보 패널의 문양 아이콘은 초상 대신 실제 문양을 표시하고 공격방식 아이콘에도 색을 적용한다. 투사체는 기존 문양 경로를 유지하며 시너지 데이터의 `VisualTowerID`도 보존한다. 기존 중첩 DebuffZone의 설정은 그대로 유지했다.

## 정리

`CubeConcepts`와 `SynergyCube`에서 참조 없는 기존 PNG **47개**, PNG 메타와 빈 폴더 메타를 제거했다. 활성 Assets에서 약 **11.18 MiB**를 줄였다. 제거 전에 모든 Assets 텍스트의 GUID 참조를 확인했으며 압축본의 모든 파일 SHA256을 원본과 대조했다.

- 복구 압축본: `removed-assets.zip` (프로젝트 루트 기준 경로를 포함)
- 연결 전 프리팹·카탈로그: `before/`
- 삭제 목록과 참조 조사: `cleanup-audit.json`
- 삭제 결과: `cleanup-result.txt`

문양 아틀라스, 물 문양, 정보 패널에서 사용하는 티어 아이콘과 흰색 아이콘, 타일, 투사체, 적·보스 큐브 면은 계속 사용하는 에셋이므로 유지했다. 에셋 재생성 스크립트의 메타 템플릿도 outputs에 따로 보관하여 삭제한 에셋에 의존하지 않게 했다.

## 검사

`checks/VisualChecks.csproj`는 **실제 TowerVisual·카탈로그 소스**를 Unity API의 최소 대체 타입과 함께 컴파일하고, 현재 프리팹·카탈로그·PNG 메타에서 읽은 참조로 검사한다. 일반 타워 280개 ID와 시너지 14개 ID, 레이어 순서, 크기 일치, 문양 색만 변경되는지, 일반↔시너지 재사용, 투사체 문양, null 데이터 처리 검사를 통과했다.

`Verify-References.ps1`은 프리팹 내부 참조와 중첩 프리팹 설정, 제거된 에셋의 GUID가 남아 있는지 확인한다. 결과는 `verification.txt`에 있다. Unity의 임포트·렌더링·플레이 모드 검증은 사용자가 수행한다. 이번 작업에서 Unity를 실행하지 않았다.
