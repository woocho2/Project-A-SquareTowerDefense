using System;
using UnityEngine;

namespace SquareTD
{
    public class WaveManager : Singleton<WaveManager>
    {
        public const int LastWave = 40;

        public int CurrentWave { get; private set; }
        public int Cycle => WaveTable.GetCycle(CurrentWave);
        public bool IsLastWave => CurrentWave == LastWave;

        public event Action<int> WaveChanged;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;
        }

        public void NextWave()
        {
            CurrentWave++;
            WaveChanged?.Invoke(CurrentWave);
        }
    }
}