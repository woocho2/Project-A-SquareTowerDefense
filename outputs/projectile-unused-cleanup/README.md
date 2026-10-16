# 미사용 Projectile 정리

`Assets/4. Asset/3. Projectile`와 구형 `Assets/10.Animation/Projectile`의 GUID 참조를 조사했다. 해당 범위 밖의 프리팹·씬·에셋에서 직접 참조하는 파일을 시작점으로 잡고, 애니메이터 → 클립 → 이미지의 의존성을 따라 사용 중인 항목을 남겼다. 코드의 Resources 로딩은 숫자 ID 프리팹을 사용하며, 그림 파일 경로를 직접 로딩하는 코드는 없음을 확인했다.

현재 연결이 없는 이미지 57개, 구형 클립 20개, 컨트롤러 20개로 총 97개를 삭제했다. 구형 컨트롤러끼리 연결된 것만으로 사용 중이라고 판단하지 않고, 범위 밖에서 실제로 참조되는지 확인했다. 메타와 비어 버린 폴더 메타까지 포함해 197개 파일을 정리했으며, 삭제 전에 `removed-assets.zip`의 모든 항목을 원본 SHA256과 비교했다.

남긴 항목은 다음과 같다.

- `Single/`: 단일 총알 14종의 아틀라스·애니메이터·Idle/Fly 클립 56개.
- `Splash/`: 무기 오브 이미지 6개와 공용 오브 애니메이터·Idle/Fly 클립 3개.
- `Cartoon/Splash/`: 현재 원소 오브 프리팹에 연결된 이미지 8개.

현재 Projectile 그림 에셋은 총 73개다. `Cartoon/Bullet`, 중복 `Projectile/Projectile`, 구형 `10.Animation/Projectile`은 비어 삭제했다. `Resources/Projectiles/101`~`114`, `201`~`214`의 28개 프리팹은 유지했다.

`audit.json`에 각 파일의 사용 여부와 참조처, `removed-files.json`에 삭제 파일의 원본 해시, `retained-hashes.json`에 유지 파일의 해시를 보관했다. 삭제된 GUID가 Assets에 남지 않았고, 유지 에셋·메타 및 Resources 프리팹·메타의 내용이 그대로임을 검사했다. 전투 코드나 활성 프리팹은 바꾸지 않았으며 Unity를 실행하지 않았다. 결과는 `verification.txt`에 있다.
