import { FileBlob, PresentationFile } from "file:///C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const sourcePath = "C:/Users/user/Downloads/20260828_조선우_네모네모 타워디펜스 초안기획서.pptx";
const candidatePath = "C:/Users/user/Desktop/프로그래밍/Project-A-SquareTowerDefense-/.codex-ppt-build/candidate.pptx";
const presentation = await PresentationFile.importPptx(await FileBlob.load(sourcePath));

const setText = (id, value) => {
  const shape = presentation.resolve(id);
  shape.text = value;
};

const setCell = (tableId, row, col, value) => {
  presentation.resolve(tableId).cells.set(row, col, value);
};

// Slide 2: revision history
setCell("tb/j65krqps", 4, 0, "4");
setCell("tb/j65krqps", 4, 1, "2026-09-06");
setCell("tb/j65krqps", 4, 2, "조선우");
setCell("tb/j65krqps", 4, 3, "현재 구현 기준 기획 내용 정리");
setCell("tb/j65krqps", 4, 4, "V0.04");

// Slide 5: actual game genre
setCell("tb/ra94bud0", 1, 1, "2D 턴제 전략 타워디펜스");

// Slide 9: tower identifiers and implemented tower types
setText("sh/ja58b650", "타워는 사각형 베젤, 내부 색상, 문양으로 구성된다.\n베젤은 브론즈, 실버, 골드, 미스릴, 다이아몬드 5단계이며 티어를 결정한다.\n색상은 공격 방식을 결정한다. 빨강은 광역, 파랑은 단일, 하양은 버프, 검정은 디버프 타워다.\n문양은 검, 활, 방패, 창, 도끼, 해머와 불, 얼음, 전기, 바람, 대지, 빛, 어둠까지 총 13종이다.\n문양은 공격력, 사거리, 보호막, 치명타, 공격 횟수, 행동력, 연쇄 등 고유 효과와 연결된다.\n타워 ID는 [티어][색][문양] 구조로 시각 요소와 데이터를 함께 결정한다.");
const towerHeaders = ["대표 문양", "검", "활", "방패", "창", "도끼", "해머", "불", "얼음", "전기"];
const towerEffects = ["관련 효과", "공격력", "사거리", "보호막", "치명타", "치명 피해", "방어 관통", "공격 횟수", "추가 대상", "연쇄 피해"];
towerHeaders.forEach((value, col) => setCell("tb/v2lgre5k", 0, col, value));
towerEffects.forEach((value, col) => setCell("tb/v2lgre5k", 1, col, value));

// Slide 12: turn-based core loop
setText("sh/b6d4f2lc", "게임은 플레이어 턴과 적 턴의 반복으로 진행된다.\n플레이어 턴은 45초 제한 시간 안에 타워 생성, 이동, 판매, 강화, 합성을 수행하거나 턴 스킵을 선택한다.\n적 턴에는 타워 공격, 적 소환과 이동, 타일 효과와 디버프 처리가 순서대로 진행된다.");

// Slide 14: controls currently implemented in the build
setText("sh/zi5c3y98", "현재 조작은 마우스 선택·드래그와 UI 버튼으로 지원");
setText("sh/xw7mts3y", "클릭: 타워 선택과 정보 표시\n드래그: 생성 타일 사이 이동\n판매 영역 드롭: 판매와 젬 획득\nUI: 생성, 강화, 턴 스킵");
presentation.resolve("sh/xw7mts3y").frame = { left: 700, top: 530, width: 270, height: 150 };
setText("sh/kzy5on2p", "키보드 단축키는\n현재 미구현");
["sh/a1wze9g7", "sh/a1gfulsf", "sh/58rytkr2", "sh/pwvy90rq"].forEach((id) => setText(id, ""));
presentation.resolve("im/f6lozqlo").delete();
["sh/ehwvat8n", "sh/sb2943y5", "sh/q5oredg3", "sh/8zexsvap", "sh/x47ep0r6", "sh/v2pwnq90"].forEach((id) => presentation.resolve(id).delete());

// Slide 16: actual wave composition
setText("sh/ip4zel83", "스테이지는 총 40웨이브로 구성된다.\n10, 20, 30, 40웨이브에는 보스 1마리가 등장한다.\n5, 15, 25, 35웨이브에는 중간보스 소환 버튼이 활성화된다.\n웨이브는 10웨이브 단위로 반복되며, 사이클이 높아질수록 적 수와 체력·방어력 배율이 증가한다.\n웨이브 완료 시 현재 웨이브 수 x 100골드를 지급한다.");
const waveRows = [
  ["WAVE", "출현 몬스터", "구현 특징"],
  ["1~3", "일반형", "사이클별 수량 증가"],
  ["4~5", "일반형·스피드형", "혼합 편성"],
  ["6~7", "일반형·방어형", "혼합 편성"],
  ["8~9", "스피드형·방어형", "혼합 편성"],
  ["10", "보스 1마리", "10웨이브 주기"],
  ["5·15·25·35", "중간보스", "버튼으로 선택 소환"],
  ["웨이브 완료", "골드 지급", "현재 웨이브 x 100"],
  ["11~20", "10웨이브 패턴 반복", "2번째 사이클"],
  ["21~30", "10웨이브 패턴 반복", "3번째 사이클"],
  ["31~40", "10웨이브 패턴 반복", "4번째 사이클"],
];
waveRows.forEach((row, rowIndex) => row.forEach((value, colIndex) => setCell("tb/18zytwfe", rowIndex, colIndex, value)));
[
  "im/e94jep47", "im/fax07u5s", "im/of61kzm9", "im/pgfid4nu", "im/ja143ipc",
  "im/y9s3ador", "im/lcjm5872", "im/kbalc36h", "im/b6hkzyp0", "im/a5836tof",
  "im/x8z21876", "im/c7ql836l", "im/v2l4b2p4",
].forEach((id) => presentation.resolve(id).delete());

// Slide 19: actual upgrade costs and caps
setText("sh/jmd83atk", "타워 강화는 색상 강화와 티어 강화로 구성된다.\n색상 강화는 같은 색상 타워의 기본 스탯을 전역으로 올리며, 비용은 현재 레벨 기준 2의 레벨승 젬이다. 레벨 5에서 최대 강화가 된다.\n티어 강화는 같은 색상과 문양을 유지한 채 한 단계 높은 티어로 교체한다. 비용은 1→2티어 5젬, 2→3티어 15젬, 3→4티어 45젬, 4→5티어 135젬이다.");

// Slide 21: keep the design intent, but identify the feature's current implementation state.
setText("sh/b6d432pc", "시너지 타워 시스템은 기획 단계의 핵심 콘텐츠다.\n4티어 이상 타워와 지정 문양 조합을 사용해 직접 타격형, 합성형, 버프형, 디버프형, 스킬형 시너지를 구성한다.\n현재 빌드에서는 시너지 전용 타일, 조합 버튼, 시너지 타워 생성 코드가 비활성화되어 있다.\n따라서 현 플레이에서는 일반 타워의 생성, 이동, 판매, 강화, 합성과 4가지 색상 공격 방식이 우선 적용된다.");

await (await PresentationFile.exportPptx(presentation)).save(candidatePath);
console.log(candidatePath);
