using System;
using System.Collections.Generic;

namespace SquareTD
{    
    public class EnemyStatusEffects
    {
        private readonly EnemyController m_owner;
        private readonly StatModifierList m_modifiers;
        private readonly EnemyResistance m_resistance;

        private readonly List<StatusEffect> m_effects = new List<StatusEffect>();
        private readonly List<Action> m_pending = new List<Action>();
        private readonly HashSet<string> m_appliedSources = new HashSet<string>();
        private int m_iterating;

        public IReadOnlyList<StatusEffect> Effects => m_effects;
        public event Action Changed;

        public EnemyStatusEffects(EnemyController owner, StatModifierList modifiers, EnemyResistance resistance)
        {
            m_owner = owner;
            m_modifiers = modifiers;
            m_resistance = resistance;
        }

        public bool Add(StatusEffect effect)
        {
            if (m_iterating > 0)
            {
                m_pending.Add(() => Add(effect));
                return true;
            }

            StatusEffect existing = Find(effect.Key);

            if (effect.IsStrongCC)
            {
                if (existing != null) return false;
                if (m_resistance != null && !m_resistance.TryStrongCC()) return false;
            }
            else if (existing != null)
            {
                if (existing.IsPermanent && !effect.IsPermanent) return false;
                bool stronger = effect.IsPermanent && !existing.IsPermanent;
                if (!stronger && effect.Strength < existing.Strength) return false;
            }

            if (existing != null)
            {
                m_effects.Remove(existing);
                existing.OnRemoved(m_owner);
            }
            m_effects.Add(effect);
            effect.OnAdded(m_owner);
            RebuildModifiers();
            return true;
        }

        public bool Remove(string key)
        {
            if (m_iterating > 0)
            {
                m_pending.Add(() => Remove(key));
                return true;
            }

            StatusEffect existing = Find(key);
            if (existing == null) return false;

            m_effects.Remove(existing);
            existing.OnRemoved(m_owner);
            RebuildModifiers();
            return true;
        }

        public int RemoveAll()
        {
            int count = m_effects.Count;
            if (count == 0) return 0;

            var removed = new List<StatusEffect>(m_effects);
            m_effects.Clear();
            foreach (StatusEffect effect in removed) effect.OnRemoved(m_owner);
            RebuildModifiers();
            return count;
        }

        public bool Has(string key)
        {
            return Find(key) != null;            
        }

        public StatusEffect Find(string key) => m_effects.Find(effect => effect.Key == key);

        private void RebuildModifiers()
        {
            foreach (string source in m_appliedSources) m_modifiers?.RemoveBySource(source);
            m_appliedSources.Clear();

            foreach (StatusEffect effect in m_effects)
            {
                foreach (StatModifier modifier in effect.GetModifiers())
                {
                    m_modifiers?.Add(modifier);
                    m_appliedSources.Add(modifier.Source);
                }
            }
            Changed?.Invoke();
        }

        private void ForEach(Action<StatusEffect> call, bool stopWhenDead = true)
        {
            m_iterating++;
            try
            {
                foreach (StatusEffect effect in m_effects)
                {
                    if (stopWhenDead && !m_owner.IsAlive) break;
                    call(effect);
                }
            }
            finally
            {
                m_iterating--;
            }
            if (m_iterating == 0) FlushPending();
        }

        private void FlushPending()
        {
            if (m_pending.Count == 0) return;
            var pending = new List<Action>(m_pending);
            m_pending.Clear();
            foreach (Action action in pending) action();
        }

        public void OnEnemyTurnStart() => ForEach(effect => effect.OnEnemyTurnStart(m_owner));
        public void OnMoved(int steps) => ForEach(effect => effect.OnMoved(m_owner, steps));
        public void OnTurn() => ForEach(effect => effect.OnTurn(m_owner));
        public void OnDamageTaken(DamageInfo info, float damage) => ForEach(effect => effect.OnDamageTaken(m_owner, info, damage), stopWhenDead: false);
        public void OnDeath(KillInfo kill) => ForEach(effect => effect.OnDeath(m_owner, kill), stopWhenDead: false);

        public MoveRange DecideMove(MoveRange range)
        {
            MoveRange result = range;
            ForEach(effect => result = effect.OnDecideMove(m_owner, result));
            return result;
        }

        public void OnBeforeHit(ref DamageInfo info)
        {
            DamageInfo result = info;
            ForEach(effect => effect.OnBeforeHit(m_owner, ref result));
            info = result;
        }

        public float ModifyIncomingDamage(DamageInfo info, float damage)
        {
            float result = damage;
            ForEach(effect => result = effect.ModifyIncomingDamage(m_owner, info, result), stopWhenDead: false);
            return result;
        }

        public void OnActed()
        {
            ForEach(effect =>
            {
                effect.CountDown();
                effect.OnActed(m_owner);
            });

            var expired = m_effects.FindAll(effect => effect.IsExpired);
            if (expired.Count == 0) return;

            foreach (StatusEffect effect in expired)
            {
                m_effects.Remove(effect);
                effect.OnExpire(m_owner);
                effect.OnRemoved(m_owner);
            }
            RebuildModifiers();
        }
    }
}
