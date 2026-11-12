using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // Shared renderer for the named skill compositions. Visuals never deal damage or roll gameplay randomness.
    [DisallowMultipleComponent]
    public sealed partial class FullSkillEffect : MonoBehaviour
    {
        private static readonly int EmblemId = Shader.PropertyToID("_Emblem");
        private static readonly int VariantId = Shader.PropertyToID("_Variant");
        private static readonly int HorizonId = Shader.PropertyToID("_Horizon");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int SwirlId = Shader.PropertyToID("_Swirl");
        private static readonly int RimId = Shader.PropertyToID("_RimColor");
        private EffectQuadPool m_motif, m_light, m_form, m_metal, m_arc, m_line;
        private EffectQuadPool m_glow, m_flare, m_ring, m_smoke, m_spark, m_ice, m_flake, m_petal, m_warp;
        private readonly Dictionary<EffectQuad, EffectQuadPool> m_leased = new Dictionary<EffectQuad, EffectQuadPool>();
        private readonly HashSet<Tween> m_tweens = new HashSet<Tween>();
        private bool m_stopping;
        private int m_generation, m_serial;
        private static readonly Color White = new Color(.94f,.98f,1);
        private static readonly Color Gold = new Color(1,.80f,.34f);
        private static readonly Color Crimson = new Color(.95f,.16f,.22f);

        private sealed class Passive
        {
            public Transform Target;
            public Func<bool> Active;
            public EffectQuad Quad;
            public Vector3 Scale, Offset;
        }
        private readonly Dictionary<(EntityId, int), Passive> m_passives = new Dictionary<(EntityId, int), Passive>();
        private readonly List<(EntityId, int)> m_gone = new List<(EntityId, int)>();

        private void EnsurePools()
        {
            if(m_motif!=null)return;
            m_motif=new EffectQuadPool(transform,"SkillMotif","Skill Form",28);
            m_light=new EffectQuadPool(transform,"SkillMotif","Skill Radiance",31,additive:true);
            m_form=new EffectQuadPool(transform,"SwordQi","Skill Weapon",30);
            m_metal=new EffectQuadPool(transform,"WeaponAttack","Skill Heavy Weapon",30);
            m_arc=new EffectQuadPool(transform,"EffectSprite","Skill Crescent",29,EffectTextures.SlashTrail,true,ArcMesh.GetCrescent(160,.30f));
            m_line=new EffectQuadPool(transform,"EffectSprite","Skill Ray",30,EffectTextures.SlashLine,true);
            m_glow=new EffectQuadPool(transform,"EffectSprite","Skill Glow",26,EffectTextures.Glow,true);
            m_flare=new EffectQuadPool(transform,"EffectSprite","Skill Flash",34,EffectTextures.Flare,true);
            m_ring=new EffectQuadPool(transform,"EffectSprite","Skill Ring",27,EffectTextures.Ring,true);
            m_smoke=new EffectQuadPool(transform,"EffectSprite","Skill Smoke",25,EffectTextures.Smoke);
            m_spark=new EffectQuadPool(transform,"EffectSprite","Skill Mote",33,EffectTextures.Spark,true);
            m_ice=new EffectQuadPool(transform,"EffectSprite","Skill Ice Shard",32,EffectTextures.IceShard,true);
            m_flake=new EffectQuadPool(transform,"EffectSprite","Skill Snow Crystal",31,EffectTextures.Snowflake,true);
            m_petal=new EffectQuadPool(transform,"EffectSprite","Skill Petal",31,EffectTextures.PlumPetal);
            m_warp=new EffectQuadPool(transform,"SpaceWarp","Skill Abyss Lens",29);
        }
        private bool Ready
        {
            get
            {
                EnsurePools();
                return !m_stopping && isActiveAndEnabled && m_motif.IsReady && m_light.IsReady && m_form.IsReady &&
                    m_metal.IsReady && m_arc.IsReady && m_line.IsReady && m_glow.IsReady && m_ring.IsReady &&
                    m_flare.IsReady && m_smoke.IsReady && m_spark.IsReady && m_ice.IsReady && m_flake.IsReady && m_petal.IsReady;
            }
        }
        private EffectQuad Acquire(EffectQuadPool pool)
        {
            EffectQuad quad=pool.Get();
            m_leased.Add(quad,pool);
            return quad;
        }
        private void Release(EffectQuad quad)
        {
            if(!m_leased.TryGetValue(quad,out var pool))return;
            m_leased.Remove(quad);
            pool.Release(quad);
        }
        private void Track(Tween tween,Action cleanup=null)
        {
            m_tweens.Add(tween);
            tween.OnKill(()=>{cleanup?.Invoke();m_tweens.Remove(tween);});
        }
        private void After(float delay,Action action)
        {
            int generation=m_generation;
            Track(DOVirtual.DelayedCall(Mathf.Max(0,delay),()=>{
                if(generation==m_generation && !m_stopping)action();
            },false).SetLink(gameObject,LinkBehaviour.KillOnDisable));
        }
        private void Animate(EffectQuad quad,float delay,float life,Action<EffectQuad,float> update)
        {
            int generation=m_generation;
            float time=0;
            quad.SetFloat(EffectQuad.AlphaId,0);
            Sequence sequence=DOTween.Sequence().SetLink(gameObject,LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(Mathf.Max(0,delay));
            sequence.Append(DOTween.To(()=>time,t=>{
                time=t;
                if(generation!=m_generation || m_stopping)return;
                quad.SetFloat(EffectQuad.AlphaId,Envelope(t));
                quad.SetFloat(EffectQuad.PhaseId,Time.time);
                update?.Invoke(quad,t);
            },1,Mathf.Max(.02f,life)).SetEase(Ease.Linear));
            Track(sequence,()=>Release(quad));
        }
        private EffectQuad Model(EffectQuadPool pool,int shape,Vector3 point,Vector3 scale,float angle,
            Color color,Color core,float delay,float life,Action<EffectQuad,float> update=null)
        {
            EffectQuad q=Acquire(pool);
            q.Transform.position=point;q.Transform.localScale=scale;
            q.Transform.rotation=Quaternion.Euler(0,0,angle);
            q.SetColor(color);q.SetColorById(EffectQuad.Color2Id,core);
            q.SetFloat(EffectQuad.ShapeId,shape);q.SetFloat(EffectQuad.ProgressId,1);
            Animate(q,delay,life,update);
            return q;
        }
        private EffectQuad Motif(int shape,TowerEmblem emblem,Vector3 point,Vector3 scale,float angle,Color color,
            float delay,float life,Action<EffectQuad,float> update=null,bool additive=true,float variant=0)
        {
            EffectQuad q=Model(additive?m_light:m_motif,shape,point,scale,angle,color,Core(emblem),delay,life,update);
            q.SetFloat(EmblemId,(int)emblem);q.SetFloat(VariantId,variant);
            return q;
        }
        private void Seal(Vector3 point,float size,TowerEmblem emblem,Color color,float delay,float life,bool ground=true)
        {
            EffectQuad q=Motif(0,emblem,point,Vector3.one*size,0,color,delay,life,(quad,t)=>{
                float grow=.65f+.35f*Out(Mathf.Min(1,t*4));
                quad.Transform.localScale=new Vector3(size*grow,size*grow*(ground? .60f:1),1);
                quad.Transform.rotation=Quaternion.Euler(0,0,ground?0:t*16);
                quad.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*4));
                quad.SetFloat(EffectQuad.AlphaId,Envelope(t)*.72f);
            });
            if(ground)q.SetSorting(SortingLayer.NameToID("MapTile"),45);
        }
        private void Sprite(EffectQuadPool pool,Vector3 point,Color color,float delay,float life,Action<EffectQuad,float> update)
        {
            EffectQuad q=Acquire(pool);q.Transform.position=point;q.SetColor(color);
            q.SetFloat(EffectQuad.IntensityId,1.3f);
            Animate(q,delay,life,update);
        }
        private void Glow(Vector3 point,float size,Color color,float delay,float life,float alpha=.7f)
        {
            Sprite(m_glow,point,color,delay,life,(q,t)=>{
                q.Transform.localScale=Vector3.one*size*(.35f+Out(t)*.65f);
                q.SetFloat(EffectQuad.AlphaId,Envelope(t)*alpha);
            });
        }
        private void Flash(Vector3 point,float size,Color color,float delay=0,float life=.18f)
        {
            Sprite(m_flare,point,color,delay,life,(q,t)=>{
                q.Transform.localScale=Vector3.one*size*(.25f+.75f*Out(Mathf.Min(1,t*4)));
                q.SetFloat(EffectQuad.AlphaId,1-t*t);
                q.SetFloat(EffectQuad.IntensityId,1.5f);
            });
        }
        private void Ring(Vector3 point,float size,Color color,float delay,float life,float flatten=.66f)
        {
            Sprite(m_ring,point,color,delay,life,(q,t)=>{
                float scale=size*(.12f+Out(t)*1.0f);
                q.Transform.localScale=new Vector3(scale,scale*flatten,1);
                q.SetFloat(EffectQuad.AlphaId,(1-t)*.85f);
            });
        }
        private void Arc(Vector3 point,float size,float angle,Color color,float delay,float life,float spin=65)
        {
            Sprite(m_arc,point,color,delay,life,(q,t)=>{
                float scale=size*(.40f+Out(t)*.18f);
                q.Transform.localScale=new Vector3(scale,scale,1);
                q.Transform.rotation=Quaternion.Euler(0,0,angle+spin*t);
                q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*5));
                q.SetFloat(EffectQuad.TailId,Mathf.Max(0,(t-.4f)/.6f));
                q.SetFloat(EffectQuad.AlphaId,Mathf.Min(1,t*8));
            });
        }
        private void Ray(Vector3 from,Vector3 to,float width,Color color,float delay,float life)
        {
            Vector3 delta=to-from;float length=delta.magnitude,angle=Angle(delta);
            Sprite(m_line,(from+to)*.5f,color,delay,life,(q,t)=>{
                float draw=Mathf.Min(1,t*5);
                q.Transform.position=from+delta*draw*.5f;
                q.Transform.localScale=new Vector3(length*draw,width,1);
                q.Transform.rotation=Quaternion.Euler(0,0,angle);
                q.SetFloat(EffectQuad.AlphaId,Envelope(t));
            });
        }
        private void Beam(Vector3 from,Vector3 to,float width,TowerEmblem emblem,Color color,float delay,float life,bool cone=false)
        {
            Motif(4,emblem,(from+to)*.5f,new Vector3(Vector3.Distance(from,to),width,1),Angle(to-from),color,delay,life,(q,t)=>{
                float draw=Mathf.Min(1,t*5);
                q.Transform.position=Vector3.Lerp(from,to,draw*.5f);
                q.Transform.localScale=new Vector3(Mathf.Max(.001f,Vector3.Distance(from,to)*draw),width*(.8f+.2f*Mathf.Sin(t*Mathf.PI)),1);
            },true,cone?1:0);
        }
        private void Bolt(Vector3 from,Vector3 to,float width,Color color,float delay,float life,int seed=0)
        {
            Motif(12,TowerEmblem.Electricity,(from+to)*.5f,
                new Vector3(Mathf.Max(.001f,Vector3.Distance(from,to)),width,1),Angle(to-from),color,delay,life,
                (q,t)=>q.SetFloat(EffectQuad.PhaseId,seed+t*life));
        }
        private void Puff(Vector3 point,Vector3 drift,float size,Color color,float delay,float life)
        {
            Sprite(m_smoke,point,color,delay,life,(q,t)=>{
                q.Transform.position=point+drift*Out(t);
                q.Transform.localScale=Vector3.one*size*(.55f+t*.8f);
                q.SetFloat(EffectQuad.AlphaId,Mathf.Sin(t*Mathf.PI)*.55f);
            });
        }
        private void Motes(Vector3 point,float radius,Color color,int count,float delay,float life,bool inward=false,EffectQuadPool pool=null)
        {
            int seed=++m_serial;
            for(int i=0;i<count;i++)
            {
                Vector3 direction=Direction(i*137.508f+seed*17);
                float reach=radius*(.45f+.55f*Hash(i+seed));
                float size=radius*(.04f+.04f*Hash(i));
                float angle=i*31f;
                Sprite(pool??m_spark,point,color,delay+i*.006f,life,(q,t)=>{
                    float move=inward?1-Out(t):Out(t);
                    q.Transform.position=point+direction*reach*move;
                    q.Transform.localScale=new Vector3(size*(1-t*.5f),size*1.7f*(1-t*.5f),1);
                    q.Transform.rotation=Quaternion.Euler(0,0,angle+t*50);
                    q.SetFloat(EffectQuad.AlphaId,Envelope(t));
                });
            }
        }
        private void Petals(Vector3 point,float radius,TowerEmblem emblem,Color color,float delay,int count=7)
        {
            for(int i=0;i<count;i++)
            {
                float angle=i*137.5f;Vector3 drift=Direction(angle)*radius;
                Motif(11,emblem,point,Vector3.one*radius*.28f,angle,color,delay+i*.018f,.60f,(q,t)=>{
                    q.Transform.position=point+drift*Out(t)+Vector3.up*radius*.25f*Mathf.Sin(t*Mathf.PI);
                    q.Transform.rotation=Quaternion.Euler(0,0,angle+t*60);
                });
            }
        }
        private void Weapon(int shape,Vector3 from,Vector3 to,float size,float delay,float life,Color color,float spin=0,bool heavy=false)
        {
            var pool=heavy?m_metal:m_form;
            Vector3 scale=shape==9||shape==10?SpearFx.Scale(size):new Vector3(size,size,1);
            Model(pool,shape,from,scale,Angle(to-from),color,Gold,delay,life,(q,t)=>{
                float move=Mathf.Min(1,t/.62f);
                q.Transform.position=Vector3.Lerp(from,to,move*move);
                q.Transform.rotation=Quaternion.Euler(0,0,spin==0?Angle(to-from):spin*t);
                if(heavy && shape==2)q.Transform.position-=q.Transform.rotation*Vector3.up*size*.26f;
                q.Transform.localScale=scale*(.7f+.3f*Mathf.Min(1,t*6));
                q.SetFloat(EffectQuad.AlphaId,Envelope(t));
            });
        }
        private void HeavyHammer(Vector3 point,float cell,float delay,float life,Color color)
        {
            float size=cell*2.5f;
            Model(m_metal,2,point,Vector3.one*size,0,color,Gold,delay,life,(q,t)=>{
                float swing=Mathf.Clamp01((t-.22f)/.35f);
                float rotate=Mathf.Lerp(-70,18,swing*swing*swing);
                Vector3 head=point+Vector3.up*cell*(.10f+1.8f*(1-swing*swing*swing));
                Quaternion rotation=Quaternion.Euler(0,0,rotate);
                q.Transform.rotation=rotation;
                q.Transform.localScale=new Vector3(size*.82f,size,1);
                q.Transform.position=head-rotation*Vector3.up*size*.26f;
                q.SetFloat(EffectQuad.AlphaId,Envelope(t));
            });
        }
        private void Impact(Vector3 point,float size,TowerEmblem emblem,Color color,float delay,bool bright=true)
        {
            Flash(point,size*.6f,bright?White:color,delay);
            Ring(point,size,color,delay,.38f);
            Glow(point,size,color,delay,.26f,.5f);
            Motes(point,size*.48f,Core(emblem),8,delay,.35f);
        }

        private void PassiveMark(Transform target,int key,int shape,TowerEmblem emblem,Vector3 scale,Color color,Func<bool> active,Vector3 offset=default)
        {
            if(target==null || !Ready)return;
            var id=(target.GetEntityId(),key);
            if(m_passives.TryGetValue(id,out var old)){Release(old.Quad);m_passives.Remove(id);}
            EffectQuad q=Acquire(m_light);
            q.SetColor(color);q.SetColorById(EffectQuad.Color2Id,Core(emblem));
            q.SetFloat(EffectQuad.ShapeId,shape);q.SetFloat(EmblemId,(int)emblem);q.SetFloat(EffectQuad.ProgressId,1);
            q.Transform.position=target.position+offset;q.Transform.localScale=scale;
            q.SetFloat(EffectQuad.AlphaId,.25f);
            // A turn-based status must remain visible without blocking the turn's "wait until VFX idle".
            EffectActivity.QuadOff(q.GameObject);
            m_passives[id]=new Passive{Target=target,Active=active,Quad=q,Scale=scale,Offset=offset};
        }
        private void LateUpdate()
        {
            m_gone.Clear();
            foreach(var pair in m_passives)
            {
                Passive p=pair.Value;
                if(p.Target==null || !p.Target.gameObject.activeInHierarchy || !p.Active())
                {m_gone.Add(pair.Key);continue;}
                p.Quad.Transform.position=p.Target.position+p.Offset;
                p.Quad.Transform.localScale=p.Scale*(1+.025f*Mathf.Sin(Time.time*2.5f));
                p.Quad.SetFloat(EffectQuad.PhaseId,Time.time*.6f);
                p.Quad.SetFloat(EffectQuad.AlphaId,.25f+.04f*Mathf.Sin(Time.time*3));
            }
            foreach(var key in m_gone){Release(m_passives[key].Quad);m_passives.Remove(key);}
        }
        private static float Envelope(float t)=>Mathf.Min(1,t*10)*Mathf.Min(1,(1-t)*5);
        private static float Out(float t)=>1-(1-t)*(1-t);
        private static float Angle(Vector3 delta)=>Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
        private static Vector3 Direction(float degrees)=>new Vector3(Mathf.Cos(degrees*Mathf.Deg2Rad),Mathf.Sin(degrees*Mathf.Deg2Rad),0);
        private static float Hash(int value)=>Mathf.Repeat(Mathf.Sin(value*127.1f+31.7f)*43758.5453f,1);
        private static Vector3 Mid(IReadOnlyList<Vector3> points,Vector3 fallback)
        {
            if(points==null || points.Count==0)return fallback;
            Vector3 sum=Vector3.zero;foreach(var p in points)sum+=p;return sum/points.Count;
        }
        private static Color Tint(TowerEmblem emblem)
        {
            switch(emblem)
            {
                case TowerEmblem.Sword:return new Color(.30f,.65f,1);
                case TowerEmblem.Axe:return new Color(.95f,.19f,.13f);
                case TowerEmblem.Bow:return new Color(.29f,.82f,.48f);
                case TowerEmblem.Spear:return new Color(.95f,.30f,.25f);
                case TowerEmblem.Shield:return new Color(.28f,.55f,1);
                case TowerEmblem.Hammer:return new Color(1,.64f,.18f);
                case TowerEmblem.Fire:return new Color(1,.27f,.035f);
                case TowerEmblem.Water:return new Color(.07f,.67f,.94f);
                case TowerEmblem.Earth:return new Color(.35f,.78f,.29f);
                case TowerEmblem.Wind:return new Color(.35f,.91f,.70f);
                case TowerEmblem.Ice:return new Color(.45f,.82f,1);
                case TowerEmblem.Electricity:return new Color(.60f,.32f,1);
                case TowerEmblem.Light:return new Color(1,.80f,.32f);
                default:return new Color(.52f,.14f,.83f);
            }
        }
        private static Color Core(TowerEmblem emblem)
        {
            if(emblem==TowerEmblem.Fire||emblem==TowerEmblem.Hammer||emblem==TowerEmblem.Light||emblem==TowerEmblem.Electricity)return new Color(1,.94f,.64f);
            if(emblem==TowerEmblem.Darkness)return new Color(.94f,.40f,1);
            return White;
        }
        private void OnEnable()=>m_stopping=false;
        private void OnDisable()
        {
            m_stopping=true;m_generation++;
            foreach(Tween tween in new List<Tween>(m_tweens))tween.Kill();
            m_tweens.Clear();m_passives.Clear();
            foreach(EffectQuad q in new List<EffectQuad>(m_leased.Keys))Release(q);
        }
        private void OnDestroy()
        {
            foreach(var pool in new[]{m_motif,m_light,m_form,m_metal,m_arc,m_line,m_glow,m_flare,m_ring,m_smoke,m_spark,m_ice,m_flake,m_petal,m_warp})pool?.Dispose();
        }
    }
}
