using System;
using System.IO;
using System.Collections.Generic;

public static class PortfolioAudioGenerator
{
    const int Rate = 44100;
    static Random rng = new Random(761118);
    static readonly string[] Emblems = { "Sword", "Axe", "Bow", "Spear", "Shield", "Hammer", "Fire", "Water", "Earth", "Wind", "Ice", "Electricity", "Light", "Darkness" };
    static readonly string[] Gods = { "Odin", "Thor", "Loki", "Hel", "Surtr", "Frey", "Baldr", "Skadi", "Freyja", "Ymir", "Ullr", "Brunhild", "BrokEitri", "Njord" };
    static readonly string[] Cues = { "Click", "Confirm", "Back", "Denied", "Build", "Merge", "Upgrade", "Wave", "Victory", "Defeat", "BuffContact", "DebuffContact" };
    static double Hz(int midi) { return 440 * Math.Pow(2, (midi - 69) / 12.0); }
    static double Noise() { return rng.NextDouble() * 2 - 1; }
    static double Ease(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }
    static void Tone(float[] a, double start, double duration, double f, double gain, int flavor)
    {
        int offset = (int)(start * Rate), count = (int)(duration * Rate);
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)Rate, x = i / (double)count;
            double envelope = Ease(t / .008) * Math.Pow(1 - x, flavor == 2 ? 1.2 : 2.4);
            double phase = 2 * Math.PI * f * t;
            double v = Math.Sin(phase + (flavor == 2 ? 1.5 * Math.Exp(-t * 8) * Math.Sin(phase * .5) : 0));
            if (flavor == 1) v = (v + .28 * Math.Sin(phase * 2.71) * Math.Exp(-t * 14) + .12 * Math.Sin(phase * 5.8) * Math.Exp(-t * 25)) / 1.4;
            if (flavor == 3) v = (v + .25 * Math.Sin(phase * 2) + .10 * Math.Sin(phase * 3)) / 1.35;
            int p = offset + i; if (p >= 0 && p < a.Length) a[p] += (float)(v * envelope * gain);
        }
    }
    static void Sweep(float[] a, double start, double duration, double low, double high, double gain, bool falling)
    {
        double lp = 0, previous = 0, hp = 0;
        int count = (int)(duration * Rate), offset = (int)(start * Rate);
        for (int i = 0; i < count; i++)
        {
            double x = i / (double)count, t = i / (double)Rate;
            double cutoff = low * Math.Pow(high / low, falling ? 1 - x : x);
            double alpha = 1 - Math.Exp(-2 * Math.PI * cutoff / Rate);
            lp += alpha * (Noise() - lp); hp = .94 * (hp + lp - previous); previous = lp;
            double envelope = Math.Pow(Math.Sin(Math.PI * x), .8) * Math.Exp(-x * 1.2);
            int p = offset + i; if (p >= 0 && p < a.Length) a[p] += (float)((hp * .65 + lp * .35) * envelope * gain);
        }
    }
    static void Thud(float[] a, double start, double duration, double pitch, double gain)
    {
        double phase = 0, filtered = 0; int offset = (int)(start * Rate);
        for (int i = 0; i < duration * Rate; i++)
        {
            double t = i / (double)Rate, x = t / duration;
            phase += 2 * Math.PI * (pitch + pitch * 2 * Math.Exp(-t * 35)) / Rate;
            filtered += .12 * (Noise() - filtered);
            double v = (Math.Sin(phase) * .75 + filtered * .4) * Ease(t / .003) * Math.Pow(1 - x, 3);
            int p = offset + i; if (p >= 0 && p < a.Length) a[p] += (float)(v * gain);
        }
    }
    static void Element(float[] a, int emblem, double time, double gain, double length, bool impact)
    {
        double d = Math.Min(length, a.Length / (double)Rate - time);
        if (d <= 0) return;
        switch (emblem)
        {
            case 0: Sweep(a,time,d,700,5200,gain,impact); Tone(a,time+.02,d*.8,impact?550:850,gain*.34,1); break;
            case 1: Sweep(a,time,d,180,2600,gain,false); Thud(a,time+d*.35,d*.6,95,gain*.75); break;
            case 2: Tone(a,time,d,220,gain*.7,3); Sweep(a,time,d,1600,5200,gain*.6,true); break;
            case 3: Sweep(a,time,d,1100,6500,gain,false); Tone(a,time+.02,d*.7,1050,gain*.24,1); break;
            case 4: Tone(a,time,d,280,gain*.7,1); Thud(a,time,d*.6,130,gain*.55); break;
            case 5: Thud(a,time,d,70,gain); Tone(a,time+.01,d,360,gain*.5,1); break;
            case 6: Sweep(a,time,d,90,2400,gain*1.4,false); Thud(a,time+.02,d*.8,75,gain*.45); for(int i=0;i<5;i++) Tone(a,time+i*d/6,.05,450+i*133,gain*.12,2); break;
            case 7: for(int i=0;i<4;i++) Tone(a,time+i*d/5,d*.55,180+i*170,gain*.4,2); Sweep(a,time,d,240,2200,gain*.45,true); break;
            case 8: Thud(a,time,d,62,gain); Sweep(a,time,d,70,900,gain*1.6,true); break;
            case 9: Sweep(a,time,d,450,4100,gain*1.15,false); break;
            case 10: for(int i=0;i<4;i++) Tone(a,time+i*d/7,d*.55,1400+i*370,gain*.25,1); Sweep(a,time,d,2400,6200,gain*.3,true); break;
            case 11: for(int i=0;i<3;i++) { Tone(a,time+i*d/4,d*.6,120+i*70,gain*.38,2); Sweep(a,time+i*d/4,d*.3,1000,7000,gain*.8,true); } break;
            case 12: Tone(a,time,d,660,gain*.6,3); Tone(a,time+.03,d*.9,990,gain*.4,1); Tone(a,time+.06,d*.8,1320,gain*.2,1); break;
            default: Tone(a,time,d,85,gain*.7,2); Sweep(a,time,d,70,1700,gain,true); Tone(a,time+.05,d*.8,127,gain*.35,2); break;
        }
    }
    static void Write(string path, float[] data, int channels, double ceiling)
    {
        double mean = 0, peak = 0; foreach(float s in data) mean += s; mean /= data.Length;
        foreach(float s in data) peak = Math.Max(peak, Math.Abs(s - mean));
        double gain = peak > 0 ? ceiling / peak : 1;
        int frames = data.Length / channels;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using(var w = new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+data.Length*2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)channels); w.Write(Rate); w.Write(Rate*channels*2); w.Write((short)(channels*2)); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(data.Length*2);
            for(int i=0;i<data.Length;i++)
            {
                int frame = i/channels; double fade = Math.Min(1,Math.Min(frame/(Rate*.004),(frames-1-frame)/(Rate*.012)));
                w.Write((short)(Math.Max(-1,Math.Min(1,(data[i]-mean)*gain*fade))*32767));
            }
        }
    }
    static void Pluck(float[] a, double start, double length, double f, double gain)
    {
        int size = Math.Max(2,(int)(Rate/f)); double[] delay = new double[size];
        for(int i=0;i<size;i++) delay[i]=Noise();
        int count=(int)(length*Rate), offset=(int)(start*Rate); double lp=0;
        for(int i=0;i<count;i++)
        {
            int p=i%size, next=(p+1)%size; double value=delay[p]; delay[p]=(delay[p]+delay[next])*.494;
            lp += .65*(value-lp); int at=(offset+i)%a.Length;
            a[at]+=(float)(lp*gain*Ease(i/(Rate*.003)));
        }
    }
    static void Music(string path, int mode)
    {
        double bpm=mode==0?100:mode==1?116:128, beat=60/bpm;
        int frames=(int)Math.Round(64*beat*Rate); float[] mono=new float[frames];
        int[] roots={50,46,53,48}; int[] scale={0,2,3,7,10,7,3,2};
        for(int bar=0;bar<16;bar++)
        {
            int root=roots[(bar/2)%4]; double at=bar*4*beat;
            for(int n=0;n<3;n++) Tone(mono,at,4*beat,Hz(root-12+(n==0?0:n==1?7:12)),.07/(n+1),3);
            for(int note=0;note<8;note++) Pluck(mono,at+note*beat*.5,2.5,Hz(root+12+scale[(note+bar%3)%scale.Length]),mode==0?.22:.14);
            if(mode>0)
            {
                for(int k=0;k<4;k++) { Thud(mono,at+k*beat,.35,mode==2?53:68,.2); if(k%2==1) Sweep(mono,at+k*beat,.16,450,2300,.12,true); }
                for(int k=0;k<8;k++) Sweep(mono,at+k*beat*.5,.055,2800,6500,.035,true);
                Tone(mono,at,beat*3.5,Hz(root),mode==2?.13:.07,3);
            }
            else if(bar%2==0) { Thud(mono,at,.55,65,.075); Tone(mono,at+2*beat,.9,Hz(root+24),.04,1); }
        }
        float[] stereo=new float[frames*2]; int lag=(int)(Rate*.173), lag2=(int)(Rate*.293);
        for(int i=0;i<frames;i++)
        {
            double fade=Math.Min(1,Math.Min(i/(Rate*.025),(frames-1-i)/(Rate*.025)));
            stereo[i*2]=(float)((mono[i]+mono[(i-lag+frames)%frames]*.19)*fade);
            stereo[i*2+1]=(float)((mono[i]+mono[(i-lag2+frames)%frames]*.19)*fade);
        }
        Write(path,stereo,2,.55);
    }
    public static void Generate(string folder)
    {
        for(int mode=0;mode<3;mode++) Music(Path.Combine(folder,"BGM/BGM_"+new[]{"Yggdrasil","Battle","Boss"}[mode]+".wav"),mode);
        for(int e=0;e<14;e++)
        {
            float[] attack=new float[(int)(Rate*.38)]; Element(attack,e,0,.7,.36,false); Write(Path.Combine(folder,"Attack/Attack_"+(e+1).ToString("00")+"_"+Emblems[e]+".wav"),attack,1,.65);
            float[] hit=new float[(int)(Rate*.28)]; Element(hit,e,0,.55,.25,true); Thud(hit,0,.10,100+e*5,.2); Write(Path.Combine(folder,"Hit/Hit_"+(e+1).ToString("00")+"_"+Emblems[e]+".wav"),hit,1,.65);
            for(int color=1;color<=4;color++)
            {
                double length=color==1?1.15:color==2?.85:.95; float[] skill=new float[(int)(Rate*length)];
                if(color<=2)
                {
                    Sweep(skill,0,length*.55,180,4000,.35,false);
                    Element(skill,e,.12,.9,length*.7,true);
                    if(color==1) { Thud(skill,.24,.65,54,.45); Element(skill,e,.35,.4,length*.5,false); }
                }
                else
                {
                    int root=color==3?62:50;
                    for(int j=0;j<4;j++) Tone(skill,j*.09,.55,Hz(root+(color==3?j*4:12-j*3)),.35, color==3?3:2);
                    Element(skill,e,.07,.22,.7,false);
                }
                Write(Path.Combine(folder,"Skill/Skill_"+(e+1).ToString("00")+"_"+Emblems[e]+"_"+new[]{"","Red","Blue","Yellow","Purple"}[color]+".wav"),skill,1,.65);
            }
        }
        for(int i=0;i<14;i++)
        {
            float[] clip=new float[(int)(Rate*1.3)]; Sweep(clip,0,.55,150,5000,.5,false); Element(clip,i,.2,.7,.8,true);
            for(int n=0;n<4;n++) Tone(clip,.1+n*.12,.7,Hz(50+(i%4)*2+n*5),.25,3);
            Write(Path.Combine(folder,"Synergy/Synergy_"+(i+1).ToString("00")+"_"+Gods[i]+".wav"),clip,1,.65);
        }
        for(int i=0;i<Cues.Length;i++)
        {
            float[] clip=new float[(int)(Rate*(i<4?.25:i<8?.65:1.25))];
            if(i==0) { Tone(clip,0,.09,960,.35,3); Thud(clip,0,.05,280,.15); }
            else if(i==2) { Tone(clip,0,.11,700,.3,3); Tone(clip,.055,.14,520,.25,3); }
            else if(i==3) { Tone(clip,0,.15,180,.35,2); Tone(clip,.04,.14,165,.2,2); }
            else if(i==9) { for(int n=0;n<4;n++) Tone(clip,n*.13,.6,Hz(57-n*3),.4,3); }
            else { int steps=i==8?6:i==1?2:4; int root=i==11?55:i==4?60:i==5?72:i==6?76:i==7?55:67; int[] chord={0,4,7,12,16,19}; for(int n=0;n<steps;n++) Tone(clip,n*.065,.38,Hz(root+(i==11?-n*2:chord[n])),.35,3); if(i==4||i==5) Thud(clip,0,.3,105,.3); }
            Write(Path.Combine(folder,"UI/"+Cues[i]+".wav"),clip,1,.60);
        }
    }
}
