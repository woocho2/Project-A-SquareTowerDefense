# 공용 스프라이트 기본용 / 9슬라이스용

`Assets/4. Asset/0. Common/`의 PNG 세 장을 각각 Multiple 모드로 설정하고, 같은 이미지 영역을 공유하는 스프라이트 두 개씩을 등록했다.

| PNG | 기본용 | 9슬라이스용 | Sliced Border (네 방향) |
| --- | --- | --- | --- |
| Board_Background.png | Board_Background_Single | Board_Background_Sliced | 80px |
| Board_Decoration.png | Board_Decoration_Single | Board_Decoration_Sliced | 160px |
| Board_Arrow.png | Board_Arrow_Single | Board_Arrow_Sliced | 96px |

기본용은 Border 0이며 기존 fileID 21300000을 유지한다. Sliced용은 fileID 21300001이며 중앙 Pivot과 PPU는 원본과 같다.
PNG 복제나 그림 변경 없이 metadata만 수정했다. 기존 타일·타워 Sprite 참조와 적 재질의 Texture 참조도 유지된다.

UI에서 9슬라이스를 쓰려면 `_Sliced` 스프라이트를 연결하고 Image의 Type을 Sliced로 지정한다. 스프라이트 이름만으로 Image Type이 자동 변경되지는 않는다.

`verification.json`에 각 스프라이트의 이름·ID·Border와 PNG 해시를 기록했다. 변경 전 meta는 `before/`에 보관했다. Unity 실행 검증은 수행하지 않았다.
