const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation');
cp.execFileSync(process.execPath,[path.join(root,'outputs/enemy-random-faces/Prepare-Validation.cjs')],{cwd:root,stdio:'inherit'});
let harness=fs.readFileSync(path.join(root,'outputs/enemy-random-faces/ValidateRandomCube.cs'),'utf8');
harness=harness.replace('yield return RandomCubeChecks.VerifyRolls(pool);','yield return RandomCubeChecks.VerifyRolls(pool);yield return HopRightingChecks.Verify(pool,manager);HopRightingChecks.RenderPreview(prefab);');
// The fixture teleports eight occupants directly to set up a full tile.
// Clear their old formation targets so Update does not undo that test-only teleport.
harness=harness.replace('start[i].transform.position=position;','start[i].transform.position=position;Set(start[i].Movement,"m_hasFormationTarget",false);');
fs.writeFileSync(path.join(__dirname,'ValidateUprightHop.cs'),harness);fs.writeFileSync(path.join(fixture,'Assets/ValidateNineSlots.cs'),harness);
fs.copyFileSync(path.join(__dirname,'HopRightingChecks.cs'),path.join(fixture,'Assets/HopRightingChecks.cs'));
console.log('Prepared upright-hop runtime checks and GPU preview renderer');
