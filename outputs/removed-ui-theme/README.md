# 기존 UI 테마 색상 기능 제거 (2026-10-19)

- UIStageThemeController.cs, UIStageThemeControllerEditor.cs, UIThemeManualColorTracker.cs 및 각 meta를 삭제했다.
- UITileSurface / UITilePanel에서 테마색, 사용자 색, 기본색을 자동으로 Image.color에 지정하는 기능을 제거했다.
- UIAtlasNameConnector의 테마색 초기화 옵션과 테마 컨트롤러 참조를 제거했다. 스프라이트 연결, 프레임 모양과 두께 설정은 유지한다.
- Canvas.prefab의 테마 컨트롤러 컴포넌트 및 Stage1 씬의 해당 컴포넌트 오버라이드를 제거했다.
- 프리팹에 남은 옛 테마 옵션 필드를 정리했다. 실제 Graphic의 m_Color 및 Image의 m_Sprite는 그대로 유지했다.
- SharedVisual.cs의 사용자 작성 코드는 수정하지 않았다. 새 SharedVisual / 에디터용 스크립트의 색상 지정 로직은 사용자가 작성한다.

`before/`에는 변경 전 프리팹·씬을 참고용으로 보관했다. `serialized-verification.json`은 프리팹 정리 대상과 색상·스프라이트 보존 확인 기록이다.
