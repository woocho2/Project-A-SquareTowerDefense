using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public sealed partial class FullSkillEffect
    {
        public void PlayBuff(TowerController owner,IReadOnlyList<Vector3> receivers)
        {
            if(owner==null||!Ready)return;
            TowerEmblem e=owner.Definition.Emblem;
            float cell=EffectSpace.CellWorldSize();Vector3 p=owner.transform.position;
            Color color=Tint(e);
            Seal(p,cell*2.1f,e,color,0,1.1f);
            Glow(p,cell*1.5f,color,.14f,.65f,.45f);
            switch(e)
            {
                case TowerEmblem.Axe: // Welcome to Valhalla: crossed axes and successive battle-call waves.
                    for(int i=0;i<2;i++)
                        Model(m_form,5,p+Vector3.up*cell*.35f,Vector3.one*cell*1.25f,i==0?-38:38,color,Gold,.12f,.9f);
                    for(int i=0;i<4;i++)Ring(p,cell*(1.3f+i*.55f),Gold,.16f+i*.13f,.55f);
                    for(int i=0;i<3;i++)Beam(p+Vector3.right*cell*(i-1)*.45f,p+Vector3.right*cell*(i-1)*.45f+Vector3.up*cell*1.6f,
                        cell*.28f,e,color,.17f+i*.05f,.7f);
                    break;
                case TowerEmblem.Bow: // Spirit bow: spectral bow crest, leaf wings, outward range rays.
                    Motif(7,e,p+Vector3.up*cell*.25f,Vector3.one*cell*2.4f,0,color,.08f,.95f);
                    Seal(p+Vector3.up*cell*.55f,cell*1.5f,e,Gold,.12f,.9f,false);
                    for(int i=0;i<5;i++)Weapon(8,p,p+Direction(25+i*32)*cell*1.1f,cell*.7f,.25f+i*.04f,.55f,color);
                    Petals(p,cell*1.4f,e,color,.20f,10);
                    break;
                case TowerEmblem.Spear: // One strike: three streams converge into one aligned spear.
                    for(int i=0;i<3;i++)Ray(p+Direction(90+i*120)*cell*1.2f,p,cell*.13f,color,.06f+i*.04f,.45f);
                    Model(m_form,9,p+Vector3.up*cell*.50f,SpearFx.Scale(cell*1.9f),90,White,Gold,.30f,.75f,
                        (q,t)=>q.Transform.localScale=SpearFx.Scale(cell*(1.3f+Out(t)*.6f)));
                    Flash(p,cell*.9f,Gold,.35f,.2f);
                    break;
                case TowerEmblem.Shield: // Aegis: four orbiting plates enclosing a protective dome.
                    Motif(2,e,p,Vector3.one*cell*2.3f,0,color,.12f,1.0f,
                        (q,t)=>q.SetFloat(EffectQuad.AlphaId,Envelope(t)*.45f));
                    for(int i=0;i<4;i++)
                    {
                        float angle=i*90;
                        Model(m_metal,0,p,Vector3.one*cell*.75f,0,color,Gold,.12f+i*.025f,1.0f,(q,t)=>{
                            q.Transform.position=p+Direction(angle+t*65)*cell*.8f+Vector3.up*cell*.12f;
                            q.Transform.rotation=Quaternion.Euler(0,0,Mathf.Sin((angle+t*65)*Mathf.Deg2Rad)*12);
                        });
                    }
                    Ring(p,cell*2.4f,Gold,.24f,.65f);
                    break;
                case TowerEmblem.Hammer: // Golden touch: stamped coins rise and turn edge-on.
                    for(int i=0;i<10;i++)
                    {
                        Vector3 start=p+new Vector3((Hash(i)-.5f)*cell*1.8f,-cell*.35f,0);
                        float phase=i*.7f;
                        Motif(0,e,start,Vector3.one*cell*.45f,0,Gold,.10f+i*.035f,.85f,(q,t)=>{
                            q.Transform.position=start+Vector3.up*cell*t*1.6f;
                            q.Transform.localScale=new Vector3(cell*.45f*(.24f+.76f*Mathf.Abs(Mathf.Cos(t*9+phase))),cell*.45f,1);
                        });
                    }
                    Flash(p,cell*1.0f,Gold,.3f);
                    break;
                case TowerEmblem.Fire: // Rising phoenix: paired flame wings and ascending feathers.
                    Motif(7,e,p+Vector3.up*cell*.15f,Vector3.one*cell*2.8f,0,color,.08f,1.1f,(q,t)=>{
                        q.Transform.position=p+Vector3.up*cell*t*.65f;
                        q.Transform.localScale=Vector3.one*cell*(2.1f+.7f*Out(t));
                    });
                    Beam(p-Vector3.up*cell*.3f,p+Vector3.up*cell*1.6f,cell*.6f,e,Gold,.20f,.8f,true);
                    Petals(p,cell*1.3f,e,Gold,.24f,12);
                    break;
                case TowerEmblem.Water: // Overflow: fountain jets, rising bubbles and expanding water rings.
                    for(int i=0;i<3;i++)
                    {
                        Vector3 start=p+Vector3.right*cell*(i-1)*.28f;
                        Beam(start-Vector3.up*cell*.3f,start+Vector3.up*cell*1.5f,cell*.55f,e,color,.1f+i*.06f,.8f,true);
                    }
                    for(int i=0;i<7;i++)
                    {
                        Vector3 start=p+Direction(i*51)*cell*.5f;
                        Motif(2,e,start,Vector3.one*cell*.28f,0,color,.15f+i*.045f,.75f,
                            (q,t)=>q.Transform.position=start+Vector3.up*cell*t*1.3f);
                    }
                    Ring(p,cell*2.0f,White,.3f,.55f);
                    break;
                case TowerEmblem.Earth: // Blossom: roots rise, buds unfurl, petals disperse.
                    Motif(8,e,p,Vector3.one*cell*2.1f,0,color,.04f,1.1f,
                        (q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*2)));
                    for(int i=0;i<5;i++)
                    {
                        Vector3 bud=p+Direction(25+i*72)*cell*.7f;
                        Motif(9,e,bud,Vector3.one*cell*.65f,i*31,new Color(1,.72f,.81f),.32f+i*.04f,.8f,
                            (q,t)=>q.Transform.localScale=Vector3.one*cell*.65f*(.15f+Out(t)*.85f));
                    }
                    Petals(p,cell*1.5f,e,color,.45f,10);
                    break;
                case TowerEmblem.Wind: // Gale force: five flowing ribbons and lifted feather-like leaves.
                    for(int i=0;i<5;i++)Arc(p,cell*(1.2f+i*.3f),i*72,color,.08f+i*.06f,.8f,150);
                    Motif(3,e,p,Vector3.one*cell*2,0,color,.12f,.90f,
                        (q,t)=>q.SetFloat(EffectQuad.AlphaId,Envelope(t)*.35f));
                    Petals(p,cell*1.4f,e,White,.25f,10);
                    break;
                case TowerEmblem.Ice: // Absolute zero: hexagonal snow seal and orbiting armour crystals.
                    Sprite(m_flake,p,color,.08f,1.05f,(q,t)=>{
                        q.Transform.localScale=Vector3.one*cell*(1.7f+.6f*Out(t));
                        q.Transform.rotation=Quaternion.Euler(0,0,t*20);
                        q.SetFloat(EffectQuad.AlphaId,Envelope(t)*.65f);
                    });
                    for(int i=0;i<6;i++)
                    {
                        float angle=i*60;
                        Sprite(m_ice,p,White,.15f+i*.03f,.90f,(q,t)=>{
                            q.Transform.position=p+Direction(angle+t*25)*cell*.85f;
                            q.Transform.rotation=Quaternion.Euler(0,0,angle-90);
                            q.Transform.localScale=new Vector3(cell*.24f,cell*.65f,1);
                        });
                    }
                    Ring(p,cell*2.2f,color,.26f,.65f);
                    break;
                case TowerEmblem.Electricity: // Hyper pulse: successive circuit pulses and forked connections.
                    for(int i=0;i<4;i++)
                    {
                        float at=.10f+i*.14f;
                        Ring(p,cell*(1.1f+i*.35f),i%2==0?color:Gold,at,.45f);
                        Bolt(p-Vector3.right*cell,p+Vector3.right*cell,cell*.6f,color,at,.30f,i);
                    }
                    Motes(p,cell*1.3f,Gold,14,.25f,.65f,true);
                    break;
                case TowerEmblem.Light: // Ascension: full wings, rising light and a halo above the caster.
                    Motif(7,e,p+Vector3.up*cell*.3f,Vector3.one*cell*3,0,Gold,.10f,1.1f);
                    Beam(p-Vector3.up*cell*.4f,p+Vector3.up*cell*2.1f,cell*.9f,e,White,.15f,.9f);
                    Ring(p+Vector3.up*cell*.95f,cell*1.2f,Gold,.20f,.90f,.35f);
                    Petals(p,cell*1.5f,e,White,.35f,12);
                    break;
                case TowerEmblem.Darkness: // Gluttony: a maw-like book gathers essence, then seals the blessing.
                    Motif(14,e,p+Vector3.up*cell*.3f,Vector3.one*cell*1.8f,0,color,.08f,1.1f);
                    for(int i=0;i<4;i++)Arc(p,cell*(1.3f+i*.2f),i*90,color,.05f+i*.06f,.85f,-130);
                    Motes(p,cell*1.7f,Core(e),18,.10f,.65f,true);
                    Flash(p,cell*.9f,Core(e),.70f,.20f);
                    break;
            }
            if(receivers==null)return;
            var points=Unique(receivers,cell);
            foreach(Vector3 target in points)
            {
                float at=.38f+Mathf.Min(.20f,Vector3.Distance(target,p)/(cell*35));
                if(e==TowerEmblem.Electricity)Bolt(p,target,cell*.35f,color,at-.12f,.35f);
                else Ray(p,target,cell*.09f,color,at-.12f,.36f);
                Blessing(target,cell,e,color,at);
            }
        }

        private void Blessing(Vector3 point,float cell,TowerEmblem e,Color color,float at)
        {
            Seal(point,cell*1.05f,e,color,at,.65f);
            Glow(point,cell*.75f,color,at+.06f,.4f,.35f);
            switch(e)
            {
                case TowerEmblem.Axe:
                    Motif(0,e,point+Vector3.up*cell*.42f,Vector3.one*cell*.6f,0,Gold,at,.65f);
                    break;
                case TowerEmblem.Bow:
                    Motif(7,e,point+Vector3.up*cell*.15f,Vector3.one*cell*1.0f,0,color,at,.65f);
                    break;
                case TowerEmblem.Spear:
                    Model(m_form,9,point+Vector3.up*cell*.25f,SpearFx.Scale(cell*.75f),90,White,Gold,at,.60f);
                    break;
                case TowerEmblem.Shield:
                    Motif(2,e,point,Vector3.one*cell*1.1f,0,color,at,.65f);
                    break;
                case TowerEmblem.Hammer:
                    Motif(0,e,point+Vector3.up*cell*.28f,Vector3.one*cell*.65f,0,Gold,at,.65f,
                        (q,t)=>q.Transform.position=point+Vector3.up*cell*(.2f+t*.4f));
                    break;
                case TowerEmblem.Fire:
                    Petals(point,cell*.55f,e,Gold,at,4);
                    break;
                case TowerEmblem.Water:
                    for(int i=0;i<2;i++)
                    {
                        Vector3 start=point+Vector3.right*cell*(i==0?-.2f: .2f);
                        Motif(2,e,start,Vector3.one*cell*.26f,0,color,at,.6f,
                            (q,t)=>q.Transform.position=start+Vector3.up*cell*t*.35f);
                    }
                    break;
                case TowerEmblem.Earth:
                    Motif(9,e,point+Vector3.up*cell*.25f,Vector3.one*cell*.65f,0,new Color(1,.76f,.82f),at,.65f);
                    break;
                case TowerEmblem.Wind:
                    for(int i=0;i<2;i++)Arc(point,cell*(.9f+i*.2f),i*180,color,at+i*.08f,.65f,140);
                    break;
                case TowerEmblem.Ice:
                    Sprite(m_flake,point+Vector3.up*cell*.30f,color,at,.65f,(q,t)=>{
                        q.Transform.localScale=Vector3.one*cell*.7f;q.Transform.rotation=Quaternion.Euler(0,0,t*30);
                    });
                    break;
                case TowerEmblem.Electricity:
                    for(int i=0;i<2;i++)Bolt(point+Direction(i*180)*cell*.4f,point+Direction(i*180+120)*cell*.4f,cell*.3f,Gold,at+i*.12f,.3f,i);
                    break;
                case TowerEmblem.Light:
                    Ring(point+Vector3.up*cell*.55f,cell*.7f,Gold,at,.65f,.35f);
                    Beam(point,point+Vector3.up*cell*.8f,cell*.3f,e,Gold,at,.5f);
                    break;
                case TowerEmblem.Darkness:
                    Motif(14,e,point+Vector3.up*cell*.3f,Vector3.one*cell*.65f,0,color,at,.6f);
                    Motes(point,cell*.55f,Core(e),4,at,.45f,true);
                    break;
            }
        }

        public void PlayDebuff(TowerController owner,IReadOnlyList<Vector3> targets)
        {
            if(owner==null||!Ready)return;
            TowerEmblem e=owner.Definition.Emblem;
            float cell=EffectSpace.CellWorldSize();Vector3 from=owner.transform.position;Color color=Tint(e);
            Seal(from,cell*2.0f,e,color,0,.85f);
            if(e==TowerEmblem.Darkness)Motif(14,e,from+Vector3.up*cell*.45f,Vector3.one*cell*1.8f,0,color,.05f,1.0f);
            else if(e==TowerEmblem.Water)
                for(int i=0;i<4;i++)Ring(from,cell*(1.3f+i*.8f),new Color(.55f,.65f,1),.1f+i*.10f,.60f);
            else if(e==TowerEmblem.Light)Beam(from,from+Vector3.up*cell*2,cell*.8f,e,Gold,.10f,.65f);
            else if(e==TowerEmblem.Earth)Motif(8,e,from,Vector3.one*cell*1.8f,0,color,.08f,.8f,(q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*3)));
            else if(e==TowerEmblem.Electricity)
                for(int i=0;i<3;i++)Bolt(from+Direction(i*120)*cell,from+Direction(i*120+100)*cell,cell*.55f,color,.1f+i*.06f,.4f,i);
            else Motes(from,cell*1.25f,Core(e),12,.05f,.55f,true);
            foreach(Vector3 point in Unique(targets,cell))
            {
                float at=.22f+Mathf.Min(.30f,Vector3.Distance(from,point)/(cell*30));
                Curse(point,from,cell,e,color,at);
            }
        }
        private void Curse(Vector3 point,Vector3 owner,float cell,TowerEmblem e,Color color,float at)
        {
            Seal(point,cell*1.1f,e,color,at,.75f);
            switch(e)
            {
                case TowerEmblem.Sword: // Doom requiem: a spectral sword descends through a broken curse cross.
                    Ray(point+Vector3.up*cell*.75f,point-Vector3.up*cell*.25f,cell*.10f,color,at,.45f);
                    Arc(point,cell*1.05f,-40,color,at+.10f,.45f,-70);
                    Arc(point,cell*.85f,140,White,at+.18f,.35f,-70);
                    break;
                case TowerEmblem.Axe: // Execution: crimson blade mark, crossing cleaves and a falling axe silhouette.
                    Weapon(5,point+Vector3.up*cell*.95f,point+Vector3.up*cell*.1f,cell*.85f,at,.55f,color,150);
                    Ray(point-Direction(45)*cell*.45f,point+Direction(45)*cell*.45f,cell*.10f,color,at+.18f,.40f);
                    Ray(point-Direction(135)*cell*.45f,point+Direction(135)*cell*.45f,cell*.10f,color,at+.22f,.40f);
                    break;
                case TowerEmblem.Bow: // Pin point: four brackets close, then an optical line focuses on the crosshair.
                    Motif(13,e,point,Vector3.one*cell*1.25f,0,color,at,.85f,(q,t)=>{
                        q.Transform.localScale=Vector3.one*cell*(1.45f-.50f*Out(Mathf.Min(1,t*3)));
                    });
                    Ray(owner,point,cell*.055f,Gold,at+.15f,.4f);
                    Flash(point,cell*.35f,Gold,at+.32f,.12f);
                    break;
                case TowerEmblem.Spear: // Hemorrhage: barbed, red tendrils and inward puncture marks.
                    Motif(8,e,point,Vector3.one*cell*1.25f,0,Crimson,at,.85f,
                        (q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*3)));
                    for(int i=0;i<2;i++)
                        Weapon(9,point+Direction(i*180+20)*cell*.8f,point,cell*.65f,at+i*.08f,.45f,Crimson);
                    break;
                case TowerEmblem.Shield: // Lockdown: chains close from four corners, seal tightens around the target.
                    Motif(10,e,point,Vector3.one*cell*1.35f,0,color,at,.85f,(q,t)=>{
                        q.Transform.localScale=Vector3.one*cell*(1.65f-.45f*Out(Mathf.Min(1,t*3)));
                    });
                    for(int i=0;i<4;i++)Ray(point+Direction(45+i*90)*cell*.9f,point,cell*.10f,White,at+i*.025f,.45f);
                    break;
                case TowerEmblem.Hammer: // Jackpot: fractured armour motif and gold-bearing fragments.
                    Motif(5,e,point,Vector3.one*cell*1.2f,0,Gold,at,.65f,(q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*4)));
                    for(int i=0;i<4;i++)
                    {
                        Vector3 drift=Direction(45+i*90)*cell*.5f;
                        Motif(6,e,point,Vector3.one*cell*.22f,i*25,Gold,at+.20f,.5f,
                            (q,t)=>q.Transform.position=point+drift*Out(t));
                    }
                    break;
                case TowerEmblem.Fire: // Inferno: four rising flame tongues and an expanding ember ring.
                    for(int i=0;i<4;i++)
                    {
                        Vector3 start=point+Vector3.right*cell*(i-1.5f)*.22f-Vector3.up*cell*.3f;
                        Beam(start,start+Vector3.up*cell*.95f,cell*.34f,e,color,at+i*.035f,.6f,true);
                    }
                    Ring(point,cell*1.25f,Gold,at+.2f,.45f);
                    Motes(point,cell*.70f,Gold,5,at+.20f,.45f);
                    break;
                case TowerEmblem.Water: // Siren song: mist and overlapping resonant ripples.
                    for(int i=0;i<3;i++)Ring(point,cell*(.65f+i*.27f),i%2==0?color:new Color(.78f,.53f,1),at+i*.12f,.55f);
                    Puff(point,Vector3.up*cell*.25f,cell*.85f,new Color(.20f,.60f,.8f,.25f),at,.8f);
                    break;
                case TowerEmblem.Earth: // Life harvest: enclosing roots draw a mote back to the caster.
                    Motif(8,e,point,Vector3.one*cell*1.35f,0,color,at,.8f,(q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*3)));
                    Arc(point,cell*1.1f,15,color,at+.15f,.45f,-100);
                    Motif(6,e,point,Vector3.one*cell*.18f,0,Gold,at+.3f,.65f,(q,t)=>{
                        q.Transform.position=Vector3.Lerp(point,owner,Out(t))+Vector3.up*cell*.25f*Mathf.Sin(t*Mathf.PI);
                    });
                    break;
                case TowerEmblem.Wind: // Turbulence: tilted funnel with sweeping cross-currents.
                    Motif(3,e,point,Vector3.one*cell*1.4f,-15,color,at,.8f);
                    for(int i=0;i<3;i++)Arc(point,cell*(.9f+i*.2f),i*120,color,at+i*.06f,.55f,170);
                    break;
                case TowerEmblem.Ice: // Deep freeze: frost seal and four ice growths, leaving the enemy readable.
                    Sprite(m_flake,point,color,at,.75f,(q,t)=>q.Transform.localScale=Vector3.one*cell*(.8f+.4f*Out(t)));
                    for(int i=0;i<4;i++)
                    {
                        Vector3 edge=point+Direction(45+i*90)*cell*.4f;
                        Sprite(m_ice,edge,White,at+i*.04f,.70f,(q,t)=>{
                            q.Transform.position=edge+Vector3.up*cell*.18f*t;
                            q.Transform.localScale=new Vector3(cell*.20f,cell*.65f*Mathf.Min(1,t*4),1);
                        });
                    }
                    break;
                case TowerEmblem.Electricity: // Static storm: top-down forks, after-discharge, circuit rings.
                    for(int i=0;i<3;i++)
                    {
                        float time=at+i*.13f;
                        Bolt(point+Vector3.up*cell*.8f+Vector3.right*cell*(i-1)*.25f,point,cell*.40f,color,time,.23f,i);
                        Flash(point,cell*.4f,Gold,time+.05f,.12f);
                    }
                    Ring(point,cell*1.2f,color,at+.12f,.65f);
                    break;
                case TowerEmblem.Light: // Banish: closing celestial focus, a white pillar, and dissolving light petals.
                    Motif(13,e,point,Vector3.one*cell*1.25f,0,Gold,at,.65f);
                    Beam(point+Vector3.up*cell*1.8f,point-Vector3.up*cell*.3f,cell*.70f,e,Gold,at+.17f,.55f);
                    Petals(point,cell*.75f,e,White,at+.3f,5);
                    break;
                case TowerEmblem.Darkness: // Dark gospel: written pages around an abyssal eye.
                    Motif(14,e,point+Vector3.up*cell*.25f,Vector3.one*cell*1.0f,0,color,at,.85f);
                    for(int i=0;i<2;i++)Arc(point,cell*(.9f+i*.2f),i*180,Core(e),at+i*.1f,.6f,-100);
                    Motes(point,cell*.7f,Core(e),5,at,.60f,true);
                    break;
            }
        }
        public void SupportContact(TowerEmblem emblem,Vector3 point,bool debuff)
        {
            if(!Ready)return;
            float cell=EffectSpace.CellWorldSize();Color color=Tint(emblem);
            Seal(point,cell*.8f,emblem,color,0,.38f);
            if(debuff)
            {
                if(emblem==TowerEmblem.Bow)Motif(13,emblem,point,Vector3.one*cell*.62f,0,color,0,.32f);
                else if(emblem==TowerEmblem.Earth||emblem==TowerEmblem.Spear)
                    Motif(8,emblem,point,Vector3.one*cell*.65f,0,color,0,.36f,(q,t)=>q.SetFloat(EffectQuad.ProgressId,Mathf.Min(1,t*4)));
                else if(emblem==TowerEmblem.Electricity)Bolt(point+Vector3.up*cell*.4f,point,cell*.25f,color,0,.23f);
                else Arc(point,cell*.65f,20,color,0,.32f,-55);
            }
            else
            {
                Glow(point,cell*.6f,color,0,.30f,.35f);
                Motes(point,cell*.32f,Core(emblem),3,0,.28f,true);
            }
        }
    }
}
