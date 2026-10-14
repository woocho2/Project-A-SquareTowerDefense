# 검 타격 이펙트 시안 v3

기준: `Assets/20. 통합 기획서.txt` 4-3 검 문양. 단일 스킬 이십사수매화검법과 광역 스킬 엑스칼리버의 시각 연출 시안이다.

- 각각 30개 고유 프레임, 12FPS, 2.5초. 아틀라스는 6열 × 5행, 셀 256×256, 투명 RGBA.
- 단일: 분홍 매화꽃잎을 동반한 연속 검기, 교차 베기 마무리, 꽃잎 소멸.
- 광역: 금빛 검 생성·낙하, 착탄 섬광과 파동, 검과 금빛 조각 소멸.
- 재생 속도 1배, 반복 꺼짐. 30개 이미지 키를 0초부터 29/12초까지 배치했다. Unity는 마지막 스프라이트 키를 한 프레임 유지하므로 실제 클립 길이는 2.5초다. 2.5초에 `DestroyEffect` 이벤트로 이펙트를 회수한다.
- 게임 피해 횟수나 스킬 로직을 변경하지 않는다. 24회 피해의 실제 처리와 시각 프레임 수는 별개다.

## 확인

`preview.html`은 이미지가 포함된 단독 미리보기로, 재생·정지·프레임 탐색·밝고 어두운 배경 확인을 지원한다. `Sword-both-preview.gif`는 왼쪽 단일, 오른쪽 광역이다. GIF 포맷의 10ms 단위 제한으로 80/80/90ms 패턴을 사용해 총 길이를 2,500ms로 맞췄다. Unity 클립과 HTML은 12FPS를 사용한다.

Unity 시안 프리팹:

- `Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Target_30_v3.prefab`
- `Assets/4. Asset/6. Effect/SwordHit_v3/Sword_Splash_30_v3.prefab`

같은 폴더에 PNG·스프라이트 분할 메타·애니메이션·컨트롤러가 있다. 각 셀은 1 유닛(256PPU)이다. 단일은 중앙 피벗, 광역은 충격파의 착탄 중심에 맞춘 피벗(0.5, 0.296875)을 사용한다.

`Assets/2. Prefab/4. Effect/EffectLibrary.asset`에 실제 게임 연결을 적용했다. 광역 검의 `HitEffectID 101`은 새 Splash 프리팹, 단일 검의 `HitEffectID 201`은 새 Target 프리팹을 재생한다. 기존 타워 CSV·투사체의 이펙트 ID를 그대로 사용하므로 모든 티어의 검 기본 공격에 적용된다. 이번 연결은 시각 효과 교체이며 스킬 발동 조건이나 24회 피해 처리를 추가하지 않는다.

연결 검증도 통과했다. 게임 EffectManager 프리팹이 사용하는 라이브러리, 두 ID의 실제 프리팹 해석, 검 광역·단일 5티어씩 총 10개 CSV 행, 착탄 피벗, 두 클립의 실제 Animator 30프레임 전환·12FPS·2.5초 길이를 확인했다. 결과는 `connection-validation.txt`에 기록했다.

## 제작과 검증

이미지는 **내장 image_gen 도구**로 제작했다. 초기 프롬프트는 `prompts.json`, 광역 배열 보정 프롬프트와 원본 경로는 `generation-manifest.json`에 기록했다. 원본은 `source/`에 보존했다. `Build-Assets.cjs`는 생성된 이미지의 프레임을 추출하고 아틀라스를 패킹하며 Unity 파일과 GIF를 만든다. 검기는 이미지를 그리는 코드로 대체하지 않았다. 최종 클립은 Unity API로 생성하고 검사했으며, 검증된 파일을 `native-clips/`와 실제 에셋 폴더에 동일하게 저장했다. 빌드 스크립트는 이 네이티브 클립을 템플릿으로 사용한다.

생성된 광역 원본은 행 간격이 균일하지 않아 투명 여백 기준으로 전체 프레임을 추출했다. 동일한 0.9배 스케일로 착탄 위치를 등록하고 정사각형 셀에 패킹했다. 프레임별 늘이기는 하지 않았다. `asset-manifest.json`에 셀과 GUID, 유효 알파 경계를 기록했다. 빌드에서 유효 픽셀이 셀 경계를 넘는지 검사한다.

`SwordVfxCheck.cs`의 `BuildAndRun()`은 Unity API로 프레임 바인딩을 저장하고 `Run()`으로 검사한다. 분리된 Unity 검증 프로젝트에서 30개 스프라이트 임포트, 순서·셀 크기·PPU, 12FPS·2.5초 길이, 1배 속도, 모든 프레임의 실제 Animator 전환 결과, 프리팹 참조, 마지막 프레임 유지 시간과 풀 반환 이벤트 검사를 통과했다. 결과 요약은 `unity-validation.txt`, 원본 로그는 `unity-validation.log`에 저장한다. 최종 에셋에 복사한 클립의 SHA256도 검증 파일과 일치한다. 게임 전투 화면에서의 크기·가독성은 시안 프리팹을 배치해 추가 조정할 수 있다.

Unity의 스프라이트 클립 생성 방식은 [공식 AnimationClip 예제](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/animationclip)를 참고했다.
