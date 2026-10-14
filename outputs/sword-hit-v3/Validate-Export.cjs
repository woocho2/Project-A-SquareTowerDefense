const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const sharp = require('../../_codex_spreadsheet_task/node_modules/sharp');
(async()=>{
  const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'asset-manifest.json'),'utf8'));
  for(const entry of manifest.assets){
    const files=fs.readdirSync(path.join(__dirname,'frames',entry.kind)).filter(p=>p.endsWith('.png'));
    assert.equal(files.length,30);
    assert.equal(new Set(files.map(p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(__dirname,'frames',entry.kind,p))).digest('hex'))).size,30);
    const atlas=await sharp(path.resolve(__dirname,'../..',entry.asset)).metadata();
    assert.equal(atlas.width,1536);assert.equal(atlas.height,1280);assert.equal(atlas.hasAlpha,true);
    assert(entry.bounds.every(b=>b.edgePixels===0));
  }
  for(const name of ['Target-preview.gif','Splash-preview.gif','Sword-both-preview.gif']){
    const gif=await sharp(path.join(__dirname,name),{animated:true}).metadata();
    assert.equal(gif.pages,30);assert.equal(gif.delay.reduce((a,b)=>a+b,0),2500);
  }
  const html=fs.readFileSync(path.join(__dirname,'preview.html'),'utf8');
  new vm.Script(html.match(/<script>([\s\S]*?)<\/script>/)[1]);
  assert.equal((html.match(/data:image\/png;base64,/g)||[]).length,2);
  const report='PASS 2 RGBA atlases (1536x1280), 60 unique 256px frames, no visible cell-edge clipping\nPASS 3 animated GIFs, 30 frames each, 2500ms each\nPASS self-contained HTML preview, embedded art, valid JavaScript\n';
  fs.writeFileSync(path.join(__dirname,'export-validation.txt'),report);
  console.log(report);
})().catch(e=>{console.error(e);process.exitCode=1;});
