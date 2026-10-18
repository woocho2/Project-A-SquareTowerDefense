const {spawnSync}=require('node:child_process'),path=require('node:path');
for(const [script,...args] of [['Build-Portraits.cjs'],['Build-Masks.cjs'],['Finalize-Layout.cjs','--capture'],['Contact.cjs'],['Preview-And-Verify.cjs']]){const result=spawnSync(process.execPath,[path.join(__dirname,script),...args],{stdio:'inherit'});if(result.status!==0)process.exit(result.status||1);}
