const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process');
const root=path.resolve(__dirname,'../..'),fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation');
cp.execFileSync(process.execPath,[path.join(root,'outputs/enemy-triple-roll/Prepare-Validation.cjs')],{cwd:root,stdio:'inherit'});
let harness=fs.readFileSync(path.join(root,'outputs/enemy-triple-roll/ValidateTripleRoll.cs'),'utf8');
harness=harness.replace('RandomCubeChecks.Verify(prefab,pool,manager);','yield return PreferredSlotChecks.Verify(pool,manager);RandomCubeChecks.Verify(prefab,pool,manager);');
fs.writeFileSync(path.join(__dirname,'ValidatePreferredSlot.cs'),harness);fs.writeFileSync(path.join(fixture,'Assets/ValidateNineSlots.cs'),harness);
fs.copyFileSync(path.join(__dirname,'PreferredSlotChecks.cs'),path.join(fixture,'Assets/PreferredSlotChecks.cs'));
fs.copyFileSync(path.join(root,'Assets/3. Scripts/04. Manager/TileManager.cs'),path.join(fixture,'Assets/GameScripts/04. Manager/TileManager.cs'));
console.log('Prepared same-slot/rightward-fallback and existing cube movement regression checks');
