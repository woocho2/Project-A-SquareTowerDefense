using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 스테이지 하나의 데이터 (이름, 일반·중간 보스·최종 보스의 이름과 그림, 스토리 문양, 라그나로크 사이클별 스테이지)
    [CreateAssetMenu(fileName = "NewStage", menuName = "SquareTD/Stage Definition")]
    public class StageDefinition : ScriptableObject
    {
        [field: SerializeField] public int Number { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public string EnglishName { get; private set; }
        [field: SerializeField] public string SceneName { get; private set; }

        public string CardTitle => $"Stage{Number}. {EnglishName}";

        [field: Header("월드 색 (게임 씬 타일맵과 메뉴 카드 미리보기에 씀)")]
        [field: SerializeField] public Color SpawnColor { get; private set; } = Color.white;
        [field: SerializeField] public Color PathColor { get; private set; } = Color.gray;
        [field: SerializeField] public Color AccentColor { get; private set; } = Color.yellow;
        [field: SerializeField] public Color FrameColor { get; private set; } = Color.yellow;

        [field: Header("판 미리보기 (Tools > SquareTD > 판 배치를 스테이지 에셋에 저장)")]
        [field: SerializeField] public Vector2Int BoardMin { get; private set; }
        [field: SerializeField] public Vector2Int BoardSize { get; private set; }
        [field: SerializeField] public List<Vector2Int> PathCells { get; private set; } = new List<Vector2Int>();
        [field: SerializeField] public List<Vector2Int> SpawnCells { get; private set; } = new List<Vector2Int>();

        [field: Header("적")]
        [field: SerializeField] public StageEnemyLook Normal { get; private set; }
        [field: SerializeField] public StageEnemyLook MiddleBoss { get; private set; }
        [field: SerializeField] public StageEnemyLook FinalBoss { get; private set; }
        [field: SerializeField] public List<TowerEmblem> StoryEmblems { get; private set; } = new List<TowerEmblem>();
        [field: SerializeField] public StageDefinition[] RagnarokCycles { get; private set; } = new StageDefinition[0];

        public bool HasLayout => PathCells != null && PathCells.Count > 0 && BoardSize.x > 0 && BoardSize.y > 0;

        public void SetLayout(Vector2Int min, Vector2Int size, List<Vector2Int> path, List<Vector2Int> spawn)
        {
            BoardMin = min;
            BoardSize = size;
            PathCells = new List<Vector2Int>(path);
            SpawnCells = new List<Vector2Int>(spawn);
        }

        public StageEnemyLook GetLook(EnemyKind kind, int cycle)
        {
            if (RagnarokCycles != null && RagnarokCycles.Length > 0)
            {
                StageDefinition source = RagnarokCycles[Mathf.Clamp(cycle - 1, 0, RagnarokCycles.Length - 1)];
                if (source != null && source != this) return source.GetLook(kind, cycle);
            }

            switch (kind)
            {
                case EnemyKind.Normal: return Normal;
                case EnemyKind.MiddleBoss: return MiddleBoss;
                case EnemyKind.FinalBoss: return FinalBoss;
                default: return null;
            }
        }
    }

    [Serializable]
    public class StageEnemyLook
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public EnemyCubeAppearance Appearance { get; private set; }
    }
}
