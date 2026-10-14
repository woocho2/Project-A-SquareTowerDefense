const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),vm=require('node:vm');
const sharp=require('../../_codex_spreadsheet_task/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),out=__dirname,fixture=path.join(root,'outputs/all-tower-assets-connection-v1/UnityValidation'),dir='Assets/4. Asset/3. Projectile/AnimatedSplash_v1';
const read=p=>fs.readFileSync(p,'utf8'),sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const check=(ok,message)=>{if(!ok)throw Error(message)};
(async()=>{
  const log=read(path.join(out,'unity-validation.log')),passes=log.split(/\r?\n/).filter(l=>l.startsWith('PASS '));
  check(passes.length===15&&passes[14].includes('all 14 single projectile regressions'),'Actual PlayMode pass report required');
  fs.writeFileSync(path.join(out,'playmode-validation.txt'),passes.join('\n')+'\n');
  const manifest=JSON.parse(read(path.join(out,'asset-manifest.json'))),guid=p=>read(p+'.meta').match(/^guid: (\w+)$/m)[1];
  for(const name of ['Orb_Idle.anim','Orb_Flight.anim','Orb_Motion.controller'])for(const suffix of ['', '.meta'])fs.copyFileSync(path.join(fixture,'Assets/OrbMotion_v1',name+suffix),path.join(root,dir,name+suffix));
  const controllerGuid=guid(path.join(root,dir,'Orb_Motion.controller'));
  for(const a of manifest.assets){
    const target=path.join(root,a.prefab),fixturePrefab=path.join(fixture,a.prefab);
    const textureGuid=guid(path.join(root,a.texture)),fixtureTextureGuid=guid(path.join(fixture,a.fixtureTexture));
    // The fixture already contains the water source under another path. Unity assigns
    // a new GUID to its duplicate; restore the real project's original texture GUID.
    check(sha(path.join(root,a.texture))===sha(path.join(fixture,a.fixtureTexture)),a.key+' fixture artwork differs');
    const nativePrefab=read(fixturePrefab).replaceAll('guid: '+fixtureTextureGuid,'guid: '+textureGuid);
    // Unity YAML's empty warning value adds a trailing space; remove only that whitespace.
    fs.writeFileSync(target,nativePrefab.replace(/^(  m_WarningMessage:)[ \t]+$/mg,'$1'));
    check(read(target).replace(/^(  m_WarningMessage:)[ \t]+$/mg,'$1')===nativePrefab.replace(/^(  m_WarningMessage:)[ \t]+$/mg,'$1'),a.key+' native prefab mismatch');
    const text=read(target);
    check(text.includes('guid: '+controllerGuid)&&text.includes('guid: '+textureGuid)&&text.includes('m_Name: OrbVisual')&&text.includes('m_usePrefabVisual: 1')&&text.includes('IsSplash: 1'),a.key+' missing main-project orb connection');
    check(text.match(/m_SortingLayerID: -1018217311/)&&text.match(/m_SortingOrder: 0/),a.key+' sorting changed');
    check(read(target+'.meta')===read(path.join(fixture,a.prefab+'.meta')),a.key+' prefab GUID changed');
    const before=read(path.join(out,'before',String(100+a.id)+'.prefab'));
    for(const pattern of [/lifeTime: .+/,/m_Radius: .+/,/m_GravityScale: .+/,/m_Mass: .+/])check(text.match(pattern)[0]===before.match(pattern)[0],a.key+' combat/physics changed');
    if(a.redrawn){check(sha(path.join(root,a.texture))===sha(path.join(out,'source',`Splash_${a.key}_Source.png`)),a.key+' generated source changed');}
    else check(sha(path.join(root,a.texture))===sha(path.join(fixture,a.fixtureTexture)),a.key+' elemental art changed');
    const {data,info}=await sharp(path.join(root,a.texture)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    let transparent=0,opaque=0;
    for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
      const alpha=data[(y*info.width+x)*4+3];if(alpha===0)transparent++;if(alpha>128)opaque++;
      // Generated PNGs contain alpha=1 rounding residue; reject visible edge content.
      if(x===0||y===0||x===info.width-1||y===info.height-1)check(alpha<=1,a.key+' visible canvas edge');
    }
    check(transparent>info.width*info.height*.15&&opaque>info.width*info.height*.15,a.key+' missing transparency/subject');
    a.guids={texture:textureGuid,controller:controllerGuid};a.sha256=sha(path.join(root,a.texture));
    if(textureGuid!==fixtureTextureGuid)a.fixtureTextureGuidRemap={from:fixtureTextureGuid,to:textureGuid};
  }
  const script=read(path.join(out,'preview.html')).match(/<script>([\s\S]*?)<\/script>/)[1];new vm.Script(script);
  manifest.connected=true;manifest.playmodeVerified=true;manifest.sharedAnimations={idle:dir+'/Orb_Idle.anim',flight:dir+'/Orb_Flight.anim',controller:dir+'/Orb_Motion.controller'};
  fs.writeFileSync(path.join(out,'asset-manifest.json'),JSON.stringify(manifest,null,2));
  fs.writeFileSync(path.join(out,'export-validation.txt'),'PASS 14 main-project prefab sprite/controller GUIDs, hierarchy, sorting and preserved combat/physics values\nPASS 6 generated weapon RGBA sources and 8 unchanged elemental PNGs, canvas edge alpha at most 1/255\nPASS native Unity clips/controllers installed and preview JavaScript parsed\n');
  console.log('Installed and validated all 14 splash orb prefabs, 2 loops and 1 shared controller');
})().catch(e=>{console.error(e);process.exitCode=1});
