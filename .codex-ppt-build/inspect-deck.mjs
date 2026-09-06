import { FileBlob, PresentationFile } from "file:///C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";

const sourcePath = "C:/Users/user/Downloads/20260828_조선우_네모네모 타워디펜스 초안기획서.pptx";
const presentation = await PresentationFile.importPptx(await FileBlob.load(sourcePath));
const snapshot = await presentation.inspect({
  kind: "textbox,table",
  include: "id,slide,text,bbox,rows,cols,preview",
  maxChars: 30000,
});
console.log(snapshot.ndjson);
