import path from "node:path";
import { pathToFileURL } from "node:url";

const skillDir = "C:/Users/user/.codex/plugins/cache/openai-primary-runtime/presentations/26.904.11930/skills/presentations";
const workspaceDir = "C:/Users/user/Desktop/프로그래밍/Project-A-SquareTowerDefense-";
const candidatePath = path.join(workspaceDir, ".codex-ppt-build", "candidate.pptx");
const finalPath = path.join(workspaceDir, "기획서_수정본", "20260828_조선우_네모네모 타워디펜스_구현반영_V0.04_최종2.pptx");
const { finalizePresentation } = await import(pathToFileURL(
  path.join(skillDir, "container_tools", "artifact_tool_utils.mjs"),
).href);

const result = await finalizePresentation({
  explicitTotalSlideCount: 21,
  workspaceDir,
  candidatePath,
  finalPath,
  pythonExecutable: "C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",
  integrityValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_package_integrity.py"),
  layoutValidatorPath: path.join(skillDir, "container_tools", "inspect_presentation_layout_geometry.py"),
  layoutArgs: ["--expected-slide-size-emu", "12192000,6858000", "--validate-bullet-geometry", "--validate-heading-fit"],
  requiredNativeTableOwnerSlides: [],
  receiptPath: path.join(workspaceDir, ".codex-ppt-build", "validation-v4.json"),
});
console.log(JSON.stringify(result));
