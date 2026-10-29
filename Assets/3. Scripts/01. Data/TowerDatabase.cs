using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SquareTD
{
    // TowerData.csv를 읽어 타워 정의를 ID로 찾게 해 줌
    public class TowerDatabase
    {
        private readonly Dictionary<int, TowerDefinition> m_byId = new Dictionary<int, TowerDefinition>();

        public int Count => m_byId.Count;
        public IEnumerable<TowerDefinition> All => m_byId.Values;

        public void Load(string csvText)
        {
            m_byId.Clear();
            string[] lines = csvText.Split('\n');
            Dictionary<string, int> columns = ReadHeader(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;

                try
                {
                    TowerDefinition definition = ParseLine(line.Split(','), columns);
                    if (m_byId.ContainsKey(definition.Id))
                    {
                        Debug.LogError($"[TowerDatabase] {i + 1}번째 줄: ID {definition.Id}가 두 번 나옴");
                        continue;
                    }
                    m_byId.Add(definition.Id, definition);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TowerDatabase] {i + 1}번째 줄을 읽지 못함: {e.Message}");
                }
            }
        }

        public TowerDefinition Get(int id)
        {
            return m_byId[id];
        }

        public bool TryGet(int id, out TowerDefinition definition)
        {
            return m_byId.TryGetValue(id, out definition);
        }

        private static Dictionary<string, int> ReadHeader(string headerLine)
        {
            var columns = new Dictionary<string, int>();
            string[] names = headerLine.Trim().Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                columns[names[i]] = i;
            }
            return columns;
        }

        private static TowerDefinition ParseLine(string[] cells, Dictionary<string, int> columns)
        {
            return new TowerDefinition(
                ReadInt(cells, columns, "ID"),
                ReadFloat(cells, columns, "Power"),
                ReadInt(cells, columns, "Range"),
                ReadInt(cells, columns, "Action"),
                ReadInt(cells, columns, "AttackCount"),
                ReadFloat(cells, columns, "CriticalRate"),
                ReadFloat(cells, columns, "CriticalDamage"),
                ReadInt(cells, columns, "Mastery"),
                ReadInt(cells, columns, "Stack"),
                ReadInt(cells, columns, "Duration"),
                ReadFloat(cells, columns, "AbilityValue"),
                ReadFloat(cells, columns, "ProjectileSpeed"),
                ReadEnum(cells, columns, "TargetPriority"));
        }

        private static string Read(string[] cells, Dictionary<string, int> columns, string name)
        {
            if (!columns.TryGetValue(name, out int index)) throw new Exception($"{name} 열이 없음");
            if (index >= cells.Length) throw new Exception($"{name} 칸이 비어 있음");
            return cells[index].Trim();
        }

        private static float ReadFloat(string[] cells, Dictionary<string, int> columns, string name)
        {
            string text = Read(cells, columns, name);
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new Exception($"{name}의 값 '{text}'이 숫자가 아님");
            return value;
        }

        private static int ReadInt(string[] cells, Dictionary<string, int> columns, string name)
        {
            string text = Read(cells, columns, name);
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new Exception($"{name}의 값 '{text}'이 숫자가 아님");
            return value;
        }

        private static TargetPriority ReadEnum(string[] cells, Dictionary<string, int> columns, string name)
        {
            string text = Read(cells, columns, name);
            if (!Enum.TryParse(text, out TargetPriority value))
                throw new Exception($"{name}의 값 '{text}'이 TargetPriority에 없음");
            return value;
        }
    }
}
