# Projectile 이름 규칙 적용

문양 번호는 검 1, 도끼 2, 활 3, 창 4, 방패 5, 망치 6, 불 7, 물 8, 대지 9, 바람 10, 얼음 11, 전기 12, 빛 13, 어둠 14다. 버전 표기는 파일·폴더·스프라이트 조각 이름에서 제거했다.

단일 총알의 위치는 `Assets/4. Asset/3. Projectile/Single/`이다. 각 문양은 아래 네 파일을 사용한다.

- `1. Sword_Projectile_Atlas.png`
- `1. Sword_Projectile_Animator.controller`
- `1. Sword_Projectile_Animation_Idle.anim`
- `1. Sword_Projectile_Animation_Fly.anim`

기존 `AnimatedSingle_v2`와 `AnimatedSplash_v1` 폴더는 `Single`, `Splash`로 변경했다. 스플래시 오브의 개별 문양 이미지도 문양 번호를 붙였으며, 여러 문양이 함께 사용하는 애니메이터와 두 클립은 `Shared_Orb_Projectile_Animator`, `Shared_Orb_Projectile_Animation_Idle`, `Shared_Orb_Projectile_Animation_Fly`로 구분했다. 공용 파일을 문양마다 복제하지 않았다.

다른 정지 이미지·기존 그림에도 문양 번호를 붙이고 구분에 필요한 Meteor, Orb, Throwing 등의 이름은 유지했다. 에셋 130개의 파일 및 내부 표시 이름을 정리했으며 파일을 삭제하지 않았다. 모든 기존 PNG 데이터, 메타 GUID, 스프라이트 fileID, 애니메이션 내용과 컨트롤러 참조를 보존했다. `Idle`/`Flight` 상태 이름과 `IsFlying` 파라미터는 코드가 사용하므로 유지했다. 파일 이름의 비행 접미사만 `_Fly`를 사용한다.

`Resources/Projectiles/101`~`214` 프리팹 경로는 런타임의 ID 기반 로딩에 쓰이므로 유지했다. 전투 코드는 바꾸지 않았다. 이미 열려 있는 Unity가 이동 중 자동 생성한 메타는 원본 메타로 복원하여 GUID와 슬라이스를 확인했다. Codex가 Unity를 실행하지는 않았다.

`rename-map.json`은 변경 경로와 원본 GUID·PNG 해시다. `before.zip`에 변경 전 전체 Projectile 그림 폴더를 보관했고, `verification.txt`에 검사 결과를 남겼다. `Normalize-Names.ps1`은 이번 일괄 변경 기록이며 완료 후 재실행하지 않는다.
