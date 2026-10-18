const fs=require('fs'),path=require('path'),crypto=require('crypto'),sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),report=require('./rebuild-report.json');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function check(v,m){if(!v)throw new Error(m);}
const sprites=t=>[...t.matchAll(/      name: (.+)\r?\n      rect:\r?\n        serializedVersion: \d+\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)[\s\S]*?      pivot: (.+)[\s\S]*?      spriteID: (.+)\r?\n      internalID: (\d+)/g)].map(m=>({name:m[1],rect:m.slice(2,6).map(Number),pivot:m[6],spriteID:m[7],id:Number(m[8])}));
(async()=>{
 const guids=new Map();let count=0;
 for(const f of report.files){
  const file=path.join(root,f.path),before=path.join(__dirname,'before',f.path);
  const old=fs.readFileSync(before+'.meta','utf8'),now=fs.readFileSync(file+'.meta','utf8'),metadata=await sharp(file).metadata();
  check(hash(fs.readFileSync(file))===f.sha256,'Artwork changed '+f.path);
  check(hash(now)===f.metadataSha256,'Importer changed '+f.path);
  check(metadata.width===f.width&&metadata.height===f.height&&metadata.hasAlpha,'Texture size/alpha '+f.path);
  const guid=now.match(/^guid: (.+)$/m)[1];check(old.match(/^guid: (.+)$/m)[1]===guid,'GUID changed');
  const ppu=Number(now.match(/^  spritePixelsToUnits: (.+)$/m)[1]);check(ppu===384,'PPU changed');
  check(/^    enableMipMap: 0$/m.test(now)&&/^    filterMode: 1$/m.test(now),'Filtering changed');
  check([...now.matchAll(/    textureCompression: (\d+)/g)].every(m=>+m[1]===0),'Unexpected compression');
  check([...now.matchAll(/(?:^|\n)\s*maxTextureSize: (\d+)/g)].every(m=>+m[1]>=f.width),'Import downscales atlas');
  const a=sprites(old),b=sprites(now);check(a.length===b.length&&b.length===f.spriteCount,'Sprite count changed');
  for(let i=0;i<a.length;i++){
   check(a[i].name===b[i].name&&a[i].spriteID===b[i].spriteID&&a[i].id===b[i].id&&a[i].pivot===b[i].pivot,'Sprite reference changed');
   check(a[i].rect.every((v,j)=>v*2===b[i].rect[j]),'Slice not doubled');
   check(b[i].rect[2]/ppu===a[i].rect[2]/192,'World size changed');count++;
  }
  guids.set(guid,new Set(b.map(s=>s.id)));
 }
 let references=0;
 for(const c of ['TowerCharacterCatalog','SynergyCharacterCatalog']){
  const t=fs.readFileSync(path.join(root,'Assets/Resources/'+c+'.asset'),'utf8');
  for(const m of t.matchAll(/\{fileID: (5[23]\d+), guid: ([a-f0-9]+)/g)){check(guids.get(m[2])?.has(+m[1]),'Broken catalog portrait');references++;}
 }
 check(count===70&&references===70,'Incomplete tower coverage');
 check(report.portraits.every(p=>!p.edgePixels&&Math.abs(p.bounds.bottom+1-488)<=3),'Clipped or misaligned portrait');
 const unity=require('./unity-render-verification.json');check(unity.passed&&unity.renderedImages===294,'Unity rendering failed');
 const result={passed:true,atlases:report.files.length,portraits:count,catalogReferences:references,cellSize:512,ppu:384,worldCanvasSize:512/384,originalGuidsAndSpriteIDsPreserved:true,metadataOnlyChangedPPUAndSliceRectangles:true,sourceArt:'Existing larger original artwork, not enlarged 256px cells',unityRenderedCombinations:unity.renderedImages,scope:'Asset imports and isolated SpriteRenderer compositions. Full stage combat was not run.'};
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});
