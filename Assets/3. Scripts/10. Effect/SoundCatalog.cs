using UnityEngine;

namespace SquareTD
{
    // 배경음악과 문양별 공격·명중·스킬·UI 사운드 표
    [CreateAssetMenu(menuName = "SquareTD/Sound Catalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        public AudioClip[] Music = new AudioClip[3];
        public AudioClip[] Ui = new AudioClip[12];
        public AudioClip[] Attacks = new AudioClip[14];
        public AudioClip[] Hits = new AudioClip[14];
        public AudioClip[] Skills = new AudioClip[56];
        public AudioClip[] Synergies = new AudioClip[14];
        public AudioClip Attack(TowerEmblem emblem) => At(Attacks, (int)emblem - 1);
        public AudioClip Hit(TowerEmblem emblem) => At(Hits, (int)emblem - 1);
        public AudioClip Skill(TowerEmblem emblem, TowerColor color) => At(Skills, ((int)emblem - 1) * 4 + (int)color - 1);
        public static AudioClip At(AudioClip[] clips, int index) => clips != null && index >= 0 && index < clips.Length ? clips[index] : null;
    }

    public enum SoundCue { Click, Confirm, Back, Denied, Build, Merge, Upgrade, Wave, Victory, Defeat, BuffContact, DebuffContact }
}
