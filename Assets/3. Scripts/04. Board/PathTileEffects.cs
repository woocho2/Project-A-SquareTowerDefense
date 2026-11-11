using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 길 위 특수 타일의 효과 (정화, 비프로스트)
    public abstract class PathTileEffect
    {
        public abstract PathTileType Type { get; }

        public virtual void OnStopped(EnemyController enemy, BoardGrid grid, SpecialTileLayout layout) { }
    }

    public class PurifyTileEffect : PathTileEffect
    {
        public override PathTileType Type => PathTileType.Purify;

        public override void OnStopped(EnemyController enemy, BoardGrid grid, SpecialTileLayout layout)
        {
            enemy.Status.RemoveAll();
            enemy.Resistance.Reset();

            Debug.Log($"[PathTile] {enemy.name} 정화 → 효과 {enemy.Status.Effects.Count}개, 저항 {enemy.Resistance.Current:0.#}%");
        }
    }

    public class BifrostTileEffect : PathTileEffect
    {
        public override PathTileType Type => PathTileType.Bifrost;

        public override void OnStopped(EnemyController enemy, BoardGrid grid, SpecialTileLayout layout)
        {
            if (layout.GetPathTile(enemy.Movement.ActionStartIndex) == PathTileType.Bifrost) return;

            enemy.Movement.TeleportTo(grid.GetBifrostPartner(enemy.PathIndex));
        }
    }

    public static class PathTileEffects
    {
        private static readonly Dictionary<PathTileType, PathTileEffect> s_effects = new Dictionary<PathTileType, PathTileEffect>
        {
            { PathTileType.Purify, new PurifyTileEffect() },
            { PathTileType.Bifrost, new BifrostTileEffect() },
        };

        public static PathTileEffect Get(PathTileType type) => s_effects.TryGetValue(type, out PathTileEffect effect) ? effect : null;
    }
}
