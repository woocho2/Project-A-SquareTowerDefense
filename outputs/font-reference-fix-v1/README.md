# 데미지 숫자 폰트 참조 복구

`Txt_Damage(Clone)`의 TMP 경고는 삭제된 폰트 GUID `4a76840c0cf414d4fb9886d23da718ab`을 참조해 폰트가 null로 읽힌 것이 원인이다. 실제 TMP 소스의 `TextMeshPro.OnPreRenderObject()`는 이 경우 렌더링 갱신마다 같은 경고를 출력한다.

현재 UI가 사용하는 `Assets/5. Font/NotoSansKR-Black SDF.asset`으로 다음 참조를 복구했다.

- `Assets/2. Prefab/Txt_Damage.prefab`: 폰트, 공유 머티리얼, MeshRenderer 머티리얼.
- `Assets/8. TextMesh Pro/Resources/TMP Settings.asset`: 기본 폰트.
- `Assets/2. Prefab/Canvas.prefab`: 끊어진 공유 머티리얼 4개와 해당 레이블의 폰트 4개.
- `Assets/1. Scenes/LoadingScene2.unity`: 로딩 문구 두 개의 폰트와 머티리얼.

활성 게임 에셋에서 이전 GUID가 남아 있지 않은지 확인했다. `_Recovery`의 복구용 장면은 수정하지 않았다. 데미지 숫자의 표기·색·풀 동작 코드는 변경하지 않았다.

분리된 Unity 프로젝트에서 데미지 프리팹의 복제본을 생성해 폰트·머티리얼·TMP 기본 폰트와 숫자 글리프를 확인하고, `1234.56`의 실제 텍스트 메시 생성을 검증했다. 결과는 `validation.txt`에 기록했다. 검사 프로젝트는 TMP 소스 패키지 대신 같은 프로젝트에서 컴파일된 DLL을 사용하므로, 검사용 복사본의 TMP 스크립트 참조만 DLL의 동일 타입으로 변환했다. 실제 게임 에셋의 패키지 스크립트 참조와 폰트 파일은 유지했다.

이미 실행 중인 풀의 데미지 숫자 객체에는 이전 참조가 남아 있을 수 있다. 플레이 모드를 종료하고 다시 실행하면 수정된 프리팹으로 객체가 생성된다.
