using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타워별 실제 피해·합성 계보를 모으고 종료 시점의 보유 기물을 고정
    public sealed class BattleResults
    {
        public sealed class Row
        {
            public readonly int Id, Tier, Order;
            public readonly string Name, Role, Location;
            public readonly double Damage;
            public readonly Sprite Portrait;
            public readonly Color TierColor;
            public Row(int id, int tier, int order, string name, string role, string location, double damage, Sprite portrait, Color tierColor)
            { Id = id; Tier = tier; Order = order; Name = name; Role = role; Location = location; Damage = damage; Portrait = portrait; TierColor = tierColor; }
        }

        public sealed class Snapshot
        {
            public readonly bool Won;
            public readonly string StageName, Mode;
            public readonly int Wave;
            public readonly IReadOnlyList<Row> Rows;
            public double TotalDamage { get; }
            public Snapshot(bool won, string stageName, string mode, int wave, List<Row> rows)
            {
                Won = won; StageName = stageName; Mode = mode; Wave = wave;
                var copy = new List<Row>(rows);
                copy.Sort((a, b) => { int damage = b.Damage.CompareTo(a.Damage); return damage != 0 ? damage : a.Order.CompareTo(b.Order); });
                Rows = copy.AsReadOnly();
                foreach (Row row in copy) TotalDamage += row.Damage;
            }
        }

        private readonly Dictionary<TowerController, double> m_damage = new Dictionary<TowerController, double>();
        private readonly Dictionary<TowerController, TowerController> m_successors = new Dictionary<TowerController, TowerController>();
        public Snapshot Final { get; private set; }

        private TowerController Resolve(TowerController tower)
        {
            while (m_successors.TryGetValue(tower, out TowerController next)) tower = next;
            return tower;
        }

        public void Record(DamageInfo info, float actualDamage)
        {
            if (Final != null || SimulationRunner.IsOn || ReferenceEquals(info.Attacker, null) || actualDamage <= 0 || float.IsNaN(actualDamage) || float.IsInfinity(actualDamage)) return;
            TowerController tower = Resolve(info.Attacker);
            m_damage.TryGetValue(tower, out double before); m_damage[tower] = before + actualDamage;
        }

        public double DamageFor(TowerController tower)
        {
            if (ReferenceEquals(tower, null)) return 0;
            return m_damage.TryGetValue(Resolve(tower), out double damage) ? damage : 0;
        }

        public void Merge(TowerController keep, IReadOnlyList<TowerController> materials, TowerController created)
        {
            if (Final != null || SimulationRunner.IsOn || created == null) return;
            Transfer(keep, created);
            foreach (TowerController material in materials) Transfer(material, created);
        }

        private void Transfer(TowerController source, TowerController target)
        {
            if (ReferenceEquals(source, null)) return;
            source = Resolve(source);
            if (ReferenceEquals(source, target)) return;
            m_damage.TryGetValue(source, out double value); m_damage.TryGetValue(target, out double before);
            m_damage[target] = before + value; m_damage.Remove(source); m_successors[source] = target;
        }

        public Snapshot Capture(bool won)
        {
            if (Final != null) return Final;
            var rows = new List<Row>();
            BoardManager board = BoardManager.Instance;
            if (board != null)
                foreach (TowerPlace place in new[] { TowerPlace.Board, TowerPlace.Bench, TowerPlace.Synergy })
                    foreach (TowerController tower in board.GetTowers(place))
                    {
                        TowerDefinition definition = tower.Definition;
                        if (definition == null) continue;
                        bool synergy = TowerId.IsSynergy(definition.Id);
                        TowerVisual visual = tower.GetComponent<TowerVisual>();
                        rows.Add(new Row(definition.Id, definition.Tier, tower.BuildOrder, EmblemNames.GetConceptName(definition.Id),
                            synergy ? "시너지" : EmblemNames.GetEmblemName(definition.Emblem) + " · " + EmblemNames.GetColorName(definition.Color),
                            (place == TowerPlace.Board ? "전장" : place == TowerPlace.Bench ? "대기석" : "시너지") + " " + (tower.SlotIndex + 1),
                            DamageFor(tower), visual != null ? visual.CharacterSprite : null, visual != null ? visual.TierColor : Color.white));
                    }
            GameSession session = GameSession.Instance;
            Final = new Snapshot(won, session != null && session.Stage != null ? session.Stage.DisplayName : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                GameSession.CurrentMode == GameMode.Hard ? "HARD" : "NORMAL", WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0, rows);
            return Final;
        }
    }
}
