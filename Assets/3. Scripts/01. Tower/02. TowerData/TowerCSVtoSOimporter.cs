using UnityEngine;
using UnityEditor;
using System.IO;

public class TowerCSVToSOImporter
{
#if UNITY_EDITOR
    // 유니티 상단 메뉴에 버튼을 생성하여 에디터에서 쉽게 실행할 수 있도록 합니다.
    [MenuItem("Tools/CSV 데이터를 타워 SO에 덮어씌우기")]

    public static void ImportTowerData()
    {
        // 1. CSV 파일 경로 설정 
        // 프로젝트 내 실제 CSV 파일이 위치한 경로로 지정해야 합니다.
        string csvPath = "Assets/12. DataCSV/TowerDataCSV.csv";

        // 파일 존재 여부 확인: 경로에 파일이 없다면 에러 로그를 띄우고 함수를 종료합니다.
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 2. CSV 파일의 모든 텍스트를 줄(Line) 단위로 읽어 배열로 저장합니다.
        string[] lines = File.ReadAllLines(csvPath);

        // 프로젝트 내에 존재하는 모든 ProjectileData 타입의 ScriptableObject 에셋을 검색합니다.
        string[] guids = AssetDatabase.FindAssets("t:TowerData");

        // 3. 첫 번째 줄(index 0)은 데이터 항목 이름(헤더)이므로 제외하고, 두 번째 줄(index 1)부터 순회합니다.
        for (int i = 1; i < lines.Length; i++)
        {
            // 각 줄을 쉼표(,)를 기준으로 분리하여 배열로 만듭니다.
            string[] values = lines[i].Split(',');

            // 데이터 열이 부족한 빈 줄이나 잘못된 줄은 건너뜁니다. (현재 9개 열 기준)
            if (values.Length < 9) continue;

            // 첫 번째 열(ID)의 값을 읽어 정수형으로 변환을 시도합니다.
            // Trim()을 사용하여 보이지 않는 공백을 제거합니다.
            if (int.TryParse(values[0].Trim(), out int csvID))
            {
                // 에디터에서 찾은 모든 SO 파일들을 하나씩 대조해봅니다.
                foreach (string guid in guids)
                {
                    // GUID를 통해 실제 파일 경로를 얻고, 해당 에셋을 메모리에 로드합니다.
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    TowerData so = AssetDatabase.LoadAssetAtPath<TowerData>(assetPath);

                    // 로드한 SO의 ID와 CSV의 ID가 일치하는 경우 데이터를 덮어씌웁니다.
                    if (so != null && so.towerID == csvID)
                    {
                        // 엑셀에서 생성될 수 있는 잉여 큰따옴표(")를 원천적으로 제거하고 공백을 없앱니다.
                        so.towerName = values[1].Replace("\"", "").Trim();

                        // 문자열을 실수(float)로 변환하여 SO 변수에 대입합니다.
                        so.towerLevel = int.Parse(values[2].Trim());
                        so.power = float.Parse(values[3].Trim());
                        so.range = float.Parse(values[4].Trim());
                        so.action = values.Length > 5 ? Mathf.Max(1, int.Parse(values[5].Trim())) : 1;
                        so.attackCount = Mathf.Max(1, Mathf.RoundToInt(float.Parse(values[6].Trim())));
                        so.splashRadius = Mathf.Max(0, Mathf.RoundToInt(float.Parse(values[7].Trim())));
                        so.additionalHitCount = values.Length > 8 ? Mathf.Max(0, int.Parse(values[8].Trim())) : 0;
                        so.isCritical = bool.Parse(values[9].Trim());
                        so.criticalRate = float.Parse(values[10].Trim());
                        so.criticalDamage = float.Parse(values[11].Trim());
                        so.duration = float.Parse(values[12].Trim());
                        so.abilityValue = float.Parse(values[13].Trim());
                        so.projectileSpeed = float.Parse(values[18].Trim());
                        so.hitEffectID = values.Length > 19 ? int.Parse(values[19].Trim()) : 0;
                        so.targetPriority = values.Length > 20 && !string.IsNullOrWhiteSpace(values[20])
                            ? ParseEnum<TargetPriority>(values[20])
                            : TargetPriority.Closest;

                        so.attackType = ParseEnum<AttackType>(values[14]);
                        string layerName = values[15].Trim();
                        so.targetLayer = !string.IsNullOrEmpty(layerName) ? LayerMask.GetMask(layerName) : 0;
                        so.buffTarget = string.IsNullOrEmpty(values[16].Trim()) ? BuffTarget.None : ParseEnum<BuffTarget>(values[16]);
                        so.debuffTarget = string.IsNullOrEmpty(values[17].Trim()) ? DebuffTarget.None : ParseEnum<DebuffTarget>(values[17]);


                        // 변경 사항이 있음을 유니티 에디터에 알려, 저장 시 파일에 반영되도록 마킹합니다.
                        EditorUtility.SetDirty(so);

                        // 일치하는 데이터를 찾았으므로 더 이상의 SO 탐색을 멈추고 다음 CSV 줄로 넘어갑니다.
                        break;
                    }
                }
            }
        }

        // 마킹된 모든 에셋들의 변경 사항을 하드디스크에 완전하게 저장합니다.
        AssetDatabase.SaveAssets();
        Debug.Log("CSV 데이터가 TowerData SO에 성공적으로 덮어씌워졌습니다.");
    }
        private static T ParseEnum<T>(string value) where T : struct
    {
        string cleanValue = value.Trim();
        if (System.Enum.TryParse(cleanValue, true, out T result))
        {
            return result;
        }
        Debug.LogWarning($"Enum 파싱 실패: {value} (기본값 0으로 설정합니다)");
        return default(T);
    }
#endif
}

