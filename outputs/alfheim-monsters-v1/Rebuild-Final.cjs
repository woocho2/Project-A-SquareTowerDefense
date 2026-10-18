const cp=require('node:child_process'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
for(const script of ['Build-Assets.cjs','Build-Masks.cjs','../character-scale-alignment-v1/Apply-Layout.cjs','Preview-And-Verify.cjs'])cp.execFileSync(process.execPath,[path.join(__dirname,script)],{cwd:root,stdio:'inherit'});
