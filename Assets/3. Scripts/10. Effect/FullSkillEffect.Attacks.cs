using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public sealed partial class FullSkillEffect
    {
        public void PlayAttack(TowerController owner,Vector3 center,IReadOnlyList<Vector3> positions)
        {
            if(owner==null || !Ready || positions==null || positions.Count==0)return;
            float cell=EffectSpace.CellWorldSize();
            float area=cell*(SplashAction.GetAreaRadius(owner.Stats.Mastery)*2+1);
            var points=Unique(positions,cell);
            TowerEmblem emblem=owner.Definition.Emblem;
            Color color=Tint(emblem);
            switch((emblem,owner.Definition.Color))
            {
                case (TowerEmblem.Shield,TowerColor.Red):
                    Seal(center,area*1.15f,emblem,color,0,.65f);
                    Model(m_metal,0,center,Vector3.one*cell*1.8f,-15,color,Gold,.12f,.50f,(q,t)=>{
                        q.Transform.position=center+Vector3.left*cell*(.4f*(1-Out(Mathf.Min(1,t*2))));
                        q.Transform.localScale=new Vector3(cell*1.5f,cell*1.8f,1)*(1+.10f*Mathf.Sin(t*Mathf.PI));
                    });
                    for(int i=0;i<3;i++)Arc(center,area*(1+i*.17f),i*120,color,.28f+i*.04f,.35f);
                    foreach(Vector3 p in points)
                    {
                        Impact(p,cell*.85f,emblem,color,.30f);
                        for(int i=0;i<3;i++)
                        {
                            float angle=i*120;
                            Motif(6,emblem,p+Vector3.up*cell*.4f,Vector3.one*cell*.18f,angle,Gold,.35f,.65f,
                                (q,t)=>q.Transform.position=p+Vector3.up*cell*.4f+Direction(angle+t*160)*cell*.25f);
                        }
                    }
                    After(.30f,()=>ScreenShake.Play(.045f,.12f));
                    break;
                case (TowerEmblem.Hammer,TowerColor.Red):
                    Seal(center,area*1.35f,emblem,Gold,0,.72f);
                    Beam(center+Vector3.up*cell*2.8f,center,cell*1.25f,emblem,Gold,.12f,.65f);
                    HeavyHammer(center,cell,0,.90f,color);
                    foreach(Vector3 p in points)
                    {
                        GroundRupture(p,cell*1.15f,.45f,emblem);
                        Impact(p,cell*1.25f,emblem,Gold,.48f);
                        Puff(p,Vector3.up*cell*.3f,cell*.85f,new Color(.65f,.51f,.32f,.5f),.48f,.55f);
                    }
                    Ring(center,area*1.5f,Gold,.46f,.48f);
                    After(.46f,()=>ScreenShake.Play(.06f,.17f));
                    JudgementTrails(center,points,cell,.48f);
                    break;
                case (TowerEmblem.Hammer,TowerColor.Blue):
                    Seal(center,cell*1.3f,emblem,Gold,0,1.3f);
                    for(int i=0;i<HammerTargetSkill.Strikes;i++)
                    {
                        float at=.18f+i*.115f;
                        Vector3 high=center+Vector3.up*cell*.85f+Direction(30+i*47)*cell*.40f;
                        Weapon(2,high,center,cell*.90f,at,.25f,color,140+i*35,true);
                        Flash(center,cell*(i==7?1.1f: .45f),Gold,at+.14f,.13f);
                        Arc(center,cell*.85f,i*63,Gold,at+.11f,.23f);
                    }
                    Model(m_metal,0,center,Vector3.one*cell*.75f,0,White,Gold,.08f,1.18f,(q,t)=>{
                        q.Transform.localScale=Vector3.one*cell*.75f*(1-.32f*t);
                        q.SetFloat(EffectQuad.AlphaId,Envelope(t)*.45f);
                    });
                    Motes(center,cell*.9f,Gold,16,1.12f,.45f);
                    Ring(center,cell*1.6f,Gold,1.1f,.40f);
                    break;
                case (TowerEmblem.Water,TowerColor.Red):
                    Tsunami(owner.transform.position,points,cell);
                    break;
                case (TowerEmblem.Earth,TowerColor.Red):
                    Earthquake(points,cell,owner.transform.position);
                    break;
                case (TowerEmblem.Earth,TowerColor.Blue):
                    Sowing(owner,center,cell);
                    break;
                case (TowerEmblem.Wind,TowerColor.Red):
                    Seal(center,area*1.15f,emblem,color,0,.6f);
                    for(int i=0;i<5;i++)
                    {
                        Arc(center,area*(.85f+i*.12f),-65+i*51,color,.18f+i*.055f,.36f,110);
                        Ray(center-Direction(i*51)*area*.6f,center+Direction(i*51)*area*.6f,
                            cell*.12f,White,.24f+i*.055f,.25f);
                    }
                    foreach(Vector3 p in points)Motes(p,cell*.55f,color,5,.25f,.35f);
                    break;
                case (TowerEmblem.Ice,TowerColor.Blue):
                    Icicles(center,cell);
                    break;
                case (TowerEmblem.Electricity,TowerColor.Red):
                    Vortex(owner.transform.position,points,cell,false);
                    break;
                case (TowerEmblem.Electricity,TowerColor.Blue):
                    Vortex(owner.transform.position,points,cell,true);
                    break;
                case (TowerEmblem.Darkness,TowerColor.Blue):
                    Abyss(center,cell);
                    break;
            }
        }

        private void Tsunami(Vector3 owner,List<Vector3> points,float cell)
        {
            float left=points[0].x,right=left,bottom=points[0].y,top=bottom;
            foreach(Vector3 p in points){left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);bottom=Mathf.Min(bottom,p.y);top=Mathf.Max(top,p.y);}
            float width=Mathf.Max(cell*2,right-left+cell*1.4f);
            float height=Mathf.Max(cell*1.8f,top-bottom+cell*1.3f);
            Vector3 middle=new Vector3((left+right)*.5f,(top+bottom)*.5f,owner.z);
            Color water=Tint(TowerEmblem.Water);
            Seal(owner,cell*1.4f,TowerEmblem.Water,water,0,.6f);
            for(int layer=0;layer<3;layer++)
            {
                int index=layer;
                Motif(1,TowerEmblem.Water,middle,new Vector3(width,height,1),0,water,.16f+layer*.07f,.80f,(q,t)=>{
                    q.Transform.position=middle+Vector3.up*height*(t-.45f)*.35f;
                    q.Transform.localScale=new Vector3(width,height*(.6f+.4f*Out(t)),1);
                    q.SetFloat(EffectQuad.PhaseId,t*2+index*.8f);
                    q.SetFloat(EffectQuad.AlphaId,Envelope(t)*(.65f-index*.13f));
                });
            }
            foreach(Vector3 p in points)
            {
                float delay=.3f+Mathf.InverseLerp(bottom,top+cell*.1f,p.y)*.35f;
                Ring(p,cell*1.25f,water,delay,.4f);
                Motes(p,cell*.65f,White,4,delay,.35f);
                for(int k=0;k<2;k++)
                    Motif(2,TowerEmblem.Water,p,Vector3.one*cell*.23f,0,water,delay+k*.035f,.4f,
                        (q,t)=>q.Transform.position=p+Vector3.up*cell*t*.7f);
            }
        }
        private void GroundRupture(Vector3 point,float size,float delay,TowerEmblem emblem)
        {
            Motif(5,emblem,point,new Vector3(size,size*.65f,1),0,new Color(.25f,.19f,.08f),delay,.65f,
                (q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*3)),false);
            for(int i=0;i<3;i++)
            {
                float angle=i*120;Vector3 drift=Direction(angle)*size*.25f;
                Model(m_form,7,point,Vector3.one*size*.34f,angle,new Color(.60f,.48f,.29f),Gold,delay+i*.018f,.55f,(q,t)=>{
                    q.Transform.position=point+drift*Out(t)+Vector3.up*size*(.5f*Mathf.Sin(t*Mathf.PI)-.16f*t);
                });
            }
        }
        private void Earthquake(List<Vector3> points,float cell,Vector3 owner)
        {
            Color earth=Tint(TowerEmblem.Earth);
            Vector3 center=Mid(points,owner);
            Seal(center,cell*3.2f,TowerEmblem.Earth,earth,0,.85f);
            for(int i=0;i<3;i++)Ring(center,cell*(2+i*1.4f),Gold,.20f+i*.12f,.6f);
            foreach(Vector3 p in points)
            {
                float at=.20f+Mathf.Min(.45f,Vector3.Distance(center,p)/(cell*15));
                GroundRupture(p,cell*1.4f,at,TowerEmblem.Earth);
                Puff(p,Vector3.up*cell*.18f,cell*.8f,new Color(.57f,.44f,.29f,.45f),at,.6f);
                Motes(p,cell*.6f,Gold,3,at,.45f);
            }
            After(.25f,()=>ScreenShake.Play(.055f,.25f));
        }
        private void Sowing(TowerController owner,Vector3 target,float cell)
        {
            Vector3 home=owner.transform.position;
            Color color=Tint(TowerEmblem.Earth);
            Motif(8,TowerEmblem.Earth,target,Vector3.one*cell*1.2f,0,color,.08f,.65f,(q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*3)));
            Impact(target,cell*.75f,TowerEmblem.Earth,color,.20f);
            Motif(6,TowerEmblem.Earth,target,Vector3.one*cell*.3f,0,Gold,.22f,.65f,(q,t)=>{
                q.Transform.position=Vector3.Lerp(target,home,Out(t))+Vector3.up*cell*.6f*Mathf.Sin(t*Mathf.PI);
                q.Transform.rotation=Quaternion.Euler(0,0,t*150);
            });
            Seal(home,cell*1.4f,TowerEmblem.Earth,color,.65f,.65f);
            Motif(9,TowerEmblem.Earth,home,Vector3.one*cell*.7f,0,color,.7f,.55f,
                (q,t)=>q.Transform.localScale=Vector3.one*cell*.7f*(.3f+Out(t)));
            Petals(home,cell*.7f,TowerEmblem.Earth,color,.7f);
        }
        private void Icicles(Vector3 center,float cell)
        {
            Color color=Tint(TowerEmblem.Ice);
            Seal(center,cell*1.4f,TowerEmblem.Ice,color,0,1.2f);
            for(int i=0;i<8;i++)
            {
                float offset=(i%3-1)*cell*.22f;
                Vector3 target=center+new Vector3(offset,0,0);
                Vector3 top=target+new Vector3(cell*.45f,cell*(1.8f+i%2*.4f),0);
                float at=.15f+i*.07f;
                Sprite(m_ice,top,color,at,.44f,(q,t)=>{
                    float drop=Mathf.Clamp01((t-.18f)/.5f);
                    q.Transform.position=Vector3.Lerp(top,target,drop*drop);
                    q.Transform.rotation=Quaternion.Euler(0,0,Angle(target-top)-90);
                    q.Transform.localScale=new Vector3(cell*.25f,cell*.95f,1)*(.65f+.35f*Mathf.Min(1,t*5));
                    q.SetFloat(EffectQuad.AlphaId,Envelope(t));
                });
                Flash(target,cell*.45f,White,at+.29f,.12f);
                Motes(target,cell*.45f,color,3,at+.30f,.30f,false,m_flake);
            }
            Ring(center,cell*1.6f,color,.90f,.45f);
            Motes(center,cell*.85f,White,12,.9f,.4f,false,m_ice);
        }
        private void Vortex(Vector3 from,List<Vector3> points,float cell,bool overload)
        {
            Color electric=Tint(TowerEmblem.Electricity);
            Vector3 center=Mid(points,from);
            After(overload? .42f: .30f,()=>ScreenShake.Play(.025f,.10f));
            if(!overload)
            {
                Seal(center,cell*2.7f,TowerEmblem.Electricity,electric,0,1.2f);
                Motif(3,TowerEmblem.Electricity,center,Vector3.one*cell*2.2f,0,electric,.10f,1.0f);
                for(int k=0;k<6;k++)
                {
                    Vector3 end=center+Direction(k*60)*cell*1.4f;
                    Bolt(center,end,cell*.6f,k%2==0?electric:Gold,.20f+k*.05f,.40f,k);
                }
            }
            foreach(Vector3 p in points)
            {
                float at=.18f+Mathf.Min(.4f,Vector3.Distance(center,p)/(cell*20));
                Seal(p,cell*1.1f,TowerEmblem.Electricity,electric,0,.55f);
                Motes(p,cell*.65f,Gold,overload?7:3,at-.08f,.3f,true);
                if(!overload)Bolt(center,p,cell*.50f,electric,at,.38f);
                for(int k=0;k<(overload?3:1);k++)
                {
                    float time=at+.20f+k*.10f;
                    Flash(p,cell*(overload? .95f: .65f),Gold,time,.16f);
                    Ring(p,cell*(.85f+k*.18f),electric,time,.32f);
                    for(int j=0;j<3;j++)
                        Bolt(p,p+Direction(j*120+k*37)*cell*.65f,cell*.35f,j%2==0?electric:Gold,time,.20f,k+j);
                }
            }
        }
        private void Abyss(Vector3 point,float cell)
        {
            Color dark=Tint(TowerEmblem.Darkness);
            Seal(point,cell*2.0f,TowerEmblem.Darkness,dark,0,.75f);
            Motes(point,cell*1.4f,Core(TowerEmblem.Darkness),14,.05f,.55f,true);
            if(m_warp.IsReady)
            {
                EffectQuad q=Model(m_warp,0,point,new Vector3(cell*1.2f,cell*2.1f,1),0,Color.black,dark,.22f,.90f,(quad,t)=>{
                    float open=Mathf.Sin(t*Mathf.PI);
                    quad.Transform.localScale=new Vector3(cell*(.10f+.95f*open),cell*(.2f+2.1f*open),1);
                    quad.SetFloat(HorizonId,.30f);quad.SetFloat(StrengthId,.7f+open*.7f);quad.SetFloat(SwirlId,4);
                });
                q.SetColorById(RimId,Core(TowerEmblem.Darkness));
            }
            else Motif(14,TowerEmblem.Darkness,point,new Vector3(cell*1.2f,cell*2,1),0,dark,.22f,.90f);
            for(int i=0;i<5;i++)Arc(point,cell*(1+i*.14f),i*72,dark,.3f+i*.045f,.55f,-140);
            Ray(point+Vector3.up*cell,point-Vector3.up*cell,cell*.10f,Core(TowerEmblem.Darkness),.95f,.22f);
            Flash(point,cell*1.15f,Core(TowerEmblem.Darkness),1.1f,.16f);
            Ring(point,cell*1.8f,dark,1.08f,.4f);
        }

        public void Rage(TowerController owner,bool preview=false)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position;
            Color color=Tint(TowerEmblem.Axe);
            Seal(p,cell*2.2f,TowerEmblem.Axe,color,0,1.15f);
            for(int i=0;i<2;i++)
                Model(m_form,5,p+Vector3.up*cell*.35f,Vector3.one*cell*1.3f,i==0?-35:35,color,Gold,.12f,.85f,(q,t)=>{
                    q.Transform.position=p+Vector3.up*cell*(.35f+t*.3f);
                });
            for(int i=0;i<3;i++)Ring(p,cell*(1.3f+i*.25f),color,.18f+i*.16f,.4f);
            Motes(p,cell*1.2f,Gold,14,.2f,.55f,true);
            if(!preview)PassiveMark(owner.transform,202,0,TowerEmblem.Axe,new Vector3(cell*1.15f,cell*.70f,1),color,
                ()=>owner!=null&&owner.State!=null&&TimedTowerBuff.GetRemaining(owner.State,AxeTargetSkill.Source)>0);
        }
        public void Rally(TowerController owner,IReadOnlyList<TowerController> allies)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position;
            Seal(p,cell*2.3f,TowerEmblem.Spear,Crimson,0,1.05f);
            Model(m_form,10,p,Vector3.one,90,White,Crimson,.06f,1.15f,(q,t)=>{
                float size=cell*(.5f+1.8f*Out(Mathf.Min(1,t*3)));
                q.Transform.localScale=SpearFx.BannerScale(size,90);
                q.Transform.position=p+Vector3.up*cell*(.45f+(.5f-t)*.25f);
            });
            for(int i=0;i<3;i++)Ring(p,cell*(1.6f+i*.9f),Crimson,.24f+i*.10f,.55f);
            if(allies!=null)foreach(var ally in allies)
            {
                if(ally==null)continue;Vector3 target=ally.transform.position;
                Ray(p,target,cell*.12f,Gold,.3f,.45f);
                Seal(target,cell*1.1f,TowerEmblem.Spear,Gold,.5f,.60f);
                Flash(target+Vector3.up*cell*.3f,cell*.45f,Gold,.55f);
            }
        }
        public void Headhunt(TowerController owner,IReadOnlyList<Vector3> path)
        {
            if(owner==null||!Ready||path==null||path.Count==0)return;
            float cell=EffectSpace.CellWorldSize();Vector3 from=owner.transform.position;
            Seal(from,cell*1.5f,TowerEmblem.Spear,Crimson,0,1.05f);
            float spacing=Mathf.Min(.075f,.90f/path.Count);
            for(int i=0;i<path.Count;i++)
            {
                Vector3 target=path[i];float at=.12f+i*spacing;
                Vector3 top=target+new Vector3(cell*(i%2==0?-.9f: .9f),cell*2.0f,0);
                Weapon(9,top,SpearFx.CenterForTip(target,(target-top).normalized,cell*1.4f,.03f),cell*1.4f,at,.58f,White);
                Ray(top,target,cell*.11f,Crimson,at+.10f,.24f);
                Flash(target,cell*.65f,Gold,at+.36f,.13f);
                Ring(target,cell*.8f,Crimson,at+.36f,.26f);
            }
        }
        public void Rebound(TowerController owner,IReadOnlyList<Vector3> path)
        {
            if(owner==null||!Ready||path==null||path.Count==0)return;
            float cell=EffectSpace.CellWorldSize();Vector3 from=owner.transform.position;
            var route=new List<Vector3>{from};foreach(var p in path)route.Add(p);route.Add(from);
            float step=Mathf.Clamp(1.5f/Mathf.Max(1,route.Count-1),.04f,.18f);
            float life=step*(route.Count-1)+.16f;
            Model(m_metal,1,from,Vector3.one*cell*.85f,0,Tint(TowerEmblem.Shield),Gold,0,life,(q,t)=>{
                float progress=Mathf.Clamp(t*life/step,0,route.Count-1.001f);
                int index=Mathf.FloorToInt(progress);float part=progress-index;
                q.Transform.position=Vector3.Lerp(route[index],route[index+1],part)+Vector3.up*cell*.14f*Mathf.Sin(part*Mathf.PI);
                q.Transform.rotation=Quaternion.Euler(0,0,t*1800);
                q.Transform.localScale=new Vector3(cell*.85f*(.70f+.30f*Mathf.Abs(Mathf.Cos(t*30))),cell*.85f,1);
            });
            for(int i=1;i<route.Count-1;i++)
            {
                Vector3 target=route[i];float at=i*step;
                if(i==1)continue; // The first point is the original contact where the rebound starts.
                Ray(route[i-1],target,cell*.12f,Gold,at-step,.20f);
                Flash(target,cell*.65f,White,at,.12f);
                Motes(target,cell*.48f,Gold,5,at,.28f);
            }
        }

        public void Breath(TowerController owner,Vector3 target,Transform follow=null)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 start=owner.transform.position;
            Color fire=Tint(TowerEmblem.Fire);
            Seal(start,cell*1.35f,TowerEmblem.Fire,fire,0,.65f);
            for(int layer=0;layer<3;layer++)
            {
                int index=layer;
                Motif(4,TowerEmblem.Fire,(start+target)*.5f,Vector3.one,0,index==2?Gold:fire,index*.035f,.70f,(q,t)=>{
                    Vector3 from=owner!=null?owner.transform.position:start;
                    Vector3 end=follow!=null?follow.position:target;
                    float length=Vector3.Distance(from,end),draw=Mathf.Min(1,t*6);
                    q.Transform.position=Vector3.Lerp(from,end,draw*.5f);
                    q.Transform.rotation=Quaternion.Euler(0,0,Angle(end-from));
                    q.Transform.localScale=new Vector3(Mathf.Max(cell*.2f,length*draw),cell*(index==2? .45f:1.1f-index*.2f),1);
                    q.SetFloat(EffectQuad.AlphaId,Envelope(t)*(index==0? .72f: .65f));
                },true,1);
            }
            Impact(target,cell*.9f,TowerEmblem.Fire,fire,.35f);
            Motes(target,cell*.75f,Gold,9,.35f,.45f);
        }
        public void Prison(TowerController owner,EnemyController enemy,bool locked,bool opening)
        {
            if(owner==null||enemy==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=enemy.transform.position;Color water=Tint(TowerEmblem.Water);
            if(opening)
            {
                Seal(p,cell*1.45f,TowerEmblem.Water,water,0,.7f);
                for(int i=0;i<3;i++)Ring(p,cell*(.7f+i*.3f),water,.12f+i*.08f,.4f);
                if(locked)
                {
                    Motif(2,TowerEmblem.Water,p,Vector3.one*cell*1.55f,0,water,.12f,.8f);
                    PassiveMark(enemy.transform,208,2,TowerEmblem.Water,Vector3.one*cell*1.35f,water,
                        ()=>enemy!=null&&enemy.IsAlive&&enemy.Status.Has(AquaPrisonEffect.PrisonKey));
                }
            }
            else
            {
                Ring(p,cell*1.25f,water,0,.40f);
                Motes(p,cell*.55f,White,6,0,.35f,true);
                Flash(p,cell*.45f,water,.2f,.13f);
            }
        }
        public void Hyperion(TowerController owner,EnemyController enemy,int stage,bool opening)
        {
            if(owner==null||enemy==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=enemy.transform.position;
            if(opening)
            {
                Seal(p,cell*1.35f,TowerEmblem.Light,Gold,0,.75f);
                Motif(13,TowerEmblem.Light,p,Vector3.one*cell*1.1f,0,Gold,.12f,.70f);
                PassiveMark(enemy.transform,213,0,TowerEmblem.Light,new Vector3(cell*.72f,cell*.45f,1),Gold,
                    ()=>enemy!=null&&enemy.IsAlive&&enemy.Status.Has(HyperionEffect.HyperionKey),Vector3.up*cell*.6f);
                return;
            }
            float power=Mathf.Min(6,stage);
            Vector3 lens=p+Vector3.up*cell*2.4f;
            Seal(lens,cell*(.65f+power*.09f),TowerEmblem.Light,Gold,0,.65f,false);
            Beam(lens,p,cell*(.8f+power*.15f),TowerEmblem.Light,Gold,.16f,.55f);
            Impact(p,cell*(1+power*.14f),TowerEmblem.Light,Gold,.24f);
            Motes(p,cell*.85f,White,9,.24f,.4f);
        }
        public void Tempest(TowerController owner,bool preview=false)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position;Color wind=Tint(TowerEmblem.Wind);
            Seal(p,cell*2,TowerEmblem.Wind,wind,0,1.0f);
            for(int i=0;i<5;i++)Arc(p,cell*(1.2f+i*.15f),i*72,wind,.08f+i*.045f,.75f,200);
            Motif(3,TowerEmblem.Wind,p,Vector3.one*cell*1.55f,0,wind,.12f,.95f,
                (q,t)=>q.SetFloat(EffectQuad.AlphaId,Envelope(t)*.45f));
            Petals(p,cell*1.1f,TowerEmblem.Wind,White,.15f,9);
            if(!preview)PassiveMark(owner.transform,210,3,TowerEmblem.Wind,Vector3.one*cell*1.15f,wind,
                ()=>owner!=null&&owner.State!=null&&TimedTowerBuff.GetRemaining(owner.State,WindTargetSkill.Source)>0);
        }
        public void Prism(TowerController owner,IReadOnlyList<TowerController> receivers)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position,lens=p+Vector3.up*cell*.8f;
            Seal(p,cell*2.1f,TowerEmblem.Light,Gold,0,1.10f);
            Motif(6,TowerEmblem.Light,lens,Vector3.one*cell*1.4f,0,Gold,.05f,1.2f,(q,t)=>{
                q.Transform.position=lens+Vector3.up*cell*.15f*Mathf.Sin(t*Mathf.PI);
                q.Transform.rotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI)*12);
            });
            Beam(p,lens,cell*.4f,TowerEmblem.Light,White,.10f,.9f);
            Color[] spectrum={new Color(1,.42f,.54f),new Color(.35f,1,.7f),new Color(.38f,.7f,1)};
            if(receivers!=null)foreach(var receiver in receivers)
            {
                if(receiver==null)continue;
                Vector3 target=receiver.transform.position;
                for(int i=0;i<3;i++)
                {
                    Vector3 offset=Vector3.up*cell*(i-1)*.08f;
                    Ray(lens+offset,target+offset,cell*.08f,spectrum[i],.35f+i*.03f,.60f);
                }
                Seal(target,cell*1.15f,TowerEmblem.Light,Gold,.62f,.60f);
                Flash(target,cell*.5f,White,.64f,.16f);
            }
        }
        public void Charge(TowerController owner)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position;
            TowerEmblem e=owner.Definition.Emblem;
            Seal(p,cell*1.2f,e,Tint(e),0,.6f);
            Motes(p,cell*.65f,Core(e),8,0,.4f,true);
        }
        public void PreviewDelayed(TowerController owner,Vector3 point)
        {
            if(owner==null||!Ready)return;
            float cell=EffectSpace.CellWorldSize();
            TowerEmblem e=owner.Definition.Emblem;
            if(e==TowerEmblem.Water)
            {
                Seal(point,cell*1.45f,e,Tint(e),0,1.05f);
                Motif(2,e,point,Vector3.one*cell*1.55f,0,Tint(e),.12f,1.15f);
                for(int i=0;i<3;i++)Ring(point,cell*(.8f+i*.25f),Tint(e),.12f+i*.13f,.5f);
            }
            else if(e==TowerEmblem.Light)
            {
                Vector3 lens=point+Vector3.up*cell*2.4f;
                Seal(lens,cell*1.1f,e,Gold,0,1.10f,false);
                Seal(point,cell*1.3f,e,Gold,0,1.2f);
                Beam(lens,point,cell*1.2f,e,Gold,.30f,.7f);
                Impact(point,cell*1.3f,e,Gold,.5f);
            }
            else
            {
                Color color=Tint(TowerEmblem.Darkness);
                Seal(point,cell*2.1f,e,color,0,1.3f);
                if(m_warp.IsReady)
                {
                    EffectQuad q=Model(m_warp,0,point,Vector3.one*cell*2.4f,0,Color.black,color,.10f,1.5f,(quad,t)=>{
                        float size=cell*2.4f*Mathf.Sin(t*Mathf.PI);
                        quad.Transform.localScale=Vector3.one*size;
                        quad.SetFloat(HorizonId,.26f);quad.SetFloat(StrengthId,.9f);quad.SetFloat(SwirlId,3.5f);
                    });
                    q.SetColorById(RimId,Core(e));
                }
                for(int i=0;i<5;i++)Arc(point,cell*(1.2f+i*.2f),i*72,color,.12f+i*.07f,.95f,-160);
                Motes(point,cell*1.5f,Core(e),16,.20f,.85f,true);
                Ring(point,cell*2.1f,color,1.35f,.4f);
            }
        }

        private void JudgementTrails(Vector3 center,List<Vector3> points,float cell,float delay)
        {
            BoardManager board=BoardManager.Instance;
            if(board==null||board.Grid==null||!board.TryGetPoint(center,out BoardPoint point)||!point.IsPath)return;
            int index=board.Grid.GetBifrostPartner(point.PathIndex);
            Vector3 landing=board.GetWorldPosition(board.Grid.GetPathCell(index));
            foreach(Vector3 p in points)
            {
                Vector3 end=landing+(p-center)*.15f;
                Motif(6,TowerEmblem.Hammer,p,Vector3.one*cell*.22f,0,Gold,delay,.55f,(q,t)=>{
                    q.Transform.position=Vector3.Lerp(p,end,Out(t))+Vector3.up*cell*.8f*Mathf.Sin(t*Mathf.PI);
                });
                Ring(end,cell*.7f,Gold,delay+.45f,.3f);
            }
        }
        private static List<Vector3> Unique(IReadOnlyList<Vector3> positions,float cell)
        {
            var result=new List<Vector3>();var keys=new HashSet<Vector2Int>();
            if(positions==null)return result;
            float unit=Mathf.Max(.001f,cell*.25f);
            foreach(Vector3 p in positions)
                if(keys.Add(new Vector2Int(Mathf.RoundToInt(p.x/unit),Mathf.RoundToInt(p.y/unit))))result.Add(p);
            return result;
        }
    }
}
