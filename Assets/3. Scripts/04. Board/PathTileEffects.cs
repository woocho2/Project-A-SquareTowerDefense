using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public abstract class PathTileEffect
    {
        public const string Source = "길 특수 타일";

        public abstract PathTileType Type { get; }

        public virtual IReadOnlyList<StatModifier> GetStandingModifiers() => Array.Empty<StatModifier>();

        public virtual void OnTurnStanding(EnemyController enemy) { }

        public virtual void OnStopped(EnemyController enemy, BoardGrid grid, SpecialTileLayout layout) { }
    }

    public class DefenseTileEffect : PathTileEffect
    {
        private static readonly StatModifier[] s_modifiers =
        {
            StatModifier.Percent(Source, StatType.Defense, 0.2f),
        };

        public override PathTileType Type => PathTileType.Defense;
        public override IReadOnlyList<StatModifier> GetStandingModifiers() => s_modifiers;
    }

    public class SpeedTileEffect : PathTileEffect
    {
        private static readonly StatModifier[] s_modifiers =
        {
            StatModifier.Flat(Source, StatType.SpeedMax, 2),
            StatModifier.Flat(Source, StatType.SpeedMin, 2),
        };

        public override PathTileType Type => PathTileType.Speed;
        public override IReadOnlyList<StatModifier> GetStandingModifiers() => s_modifiers;
    }

    public class ActionCountTileEffect : PathTileEffect
    {
        public const int Amount = 2;

        public override PathTileType Type => PathTileType.ActionCount;

        public override void OnTurnStanding(EnemyController enemy)
        {
            enemy.Movement.AddActionProgress(Amount);
        }
    }

    public class HealTileEffect : PathTileEffect
    {
        public const float Rate = 0.05f;

        public override PathTileType Type => PathTileType.Heal;

        public override void OnTurnStanding(EnemyController enemy)
        {
            enemy.Health.Heal(enemy.MaxHp * Rate);
        }
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
            { PathTileType.Defense, new DefenseTileEffect() },
            { PathTileType.Speed, new SpeedTileEffect() },
            { PathTileType.ActionCount, new ActionCountTileEffect() },
            { PathTileType.Heal, new HealTileEffect() },
            { PathTileType.Purify, new PurifyTileEffect() },
            { PathTileType.Bifrost, new BifrostTileEffect() },
        };

        public static PathTileEffect Get(PathTileType type) => s_effects.TryGetValue(type, out PathTileEffect effect) ? effect : null;     // None이면 null
    }
}
