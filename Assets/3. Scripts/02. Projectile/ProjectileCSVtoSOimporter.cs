using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectileCSVToSOImporter
{
#if UNITY_EDITOR
    // 유니티 상단 메뉴에 버튼을 생성하여 에디터에서 쉽게 실행할 수 있도록 합니다.
    [MenuItem("Tools/CSV 데이터를 투사체 SO에 덮어씌우기")]
    public static void ImportProjectileData()
    {
        // 1. CSV 파일 경로 설정 
        // 프로젝트 내 실제 CSV 파일이 위치한 경로로 지정해야 합니다.
        string csvPath = "Assets/12. DataCSV/ProjectileDataCSV.csv";

        // 파일 존재 여부 확인: 경로에 파일이 없다면 에러 로그를 띄우고 함수를 종료합니다.
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 2. CSV 파일의 모든 텍스트를 줄(Line) 단위로 읽어 배열로 저장합니다.
        string[] lines = File.ReadAllLines(csvPath);

        // 프로젝트 내에 존재하는 모든 ProjectileData 타입의 ScriptableObject 에셋을 검색합니다.
        string[] guids = AssetDatabase.FindAssets("t:ProjectileData");

        // 3. 첫 번째 줄(index 0)은 데이터 항목 이름(헤더)이므로 제외하고, 두 번째 줄(index 1)부터 순회합니다.
        for (int i = 1; i < lines.Length; i++)
        {
            // 각 줄을 쉼표(,)를 기준으로 분리하여 배열로 만듭니다.
            string[] values = lines[i].Split(',');

            // 데이터 열이 부족한 빈 줄이나 잘못된 줄은 건너뜁니다. (현재 9개 열 기준)
            if (values.Length < 5) continue;

            // 첫 번째 열(ID)의 값을 읽어 정수형으로 변환을 시도합니다.
            // Trim()을 사용하여 보이지 않는 공백을 제거합니다.
            if (int.TryParse(values[0].Trim(), out int csvID))
            {
                // 에디터에서 찾은 모든 SO 파일들을 하나씩 대조해봅니다.
                foreach (string guid in guids)
                {
                    // GUID를 통해 실제 파일 경로를 얻고, 해당 에셋을 메모리에 로드합니다.
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    ProjectileData so = AssetDatabase.LoadAssetAtPath<ProjectileData>(assetPath);

                    // 로드한 SO의 ID와 CSV의 ID가 일치하는 경우 데이터를 덮어씌웁니다.
                    if (so != null && so.projectileID == csvID)
                    {
                        so.projectileName = values[1].Replace("\"", "").Trim();
                        so.speed = float.Parse(values[2].Trim());
                        so.splashradius = float.Parse(values[3].Trim());
                        so.hiteffectID = int.Parse(values[4].Trim());

                        EditorUtility.SetDirty(so);

                        break;
                    }
                }
            }
        }

        // 마킹된 모든 에셋들의 변경 사항을 하드디스크에 완전하게 저장합니다.
        AssetDatabase.SaveAssets();
        Debug.Log("CSV 데이터가 ProjectileData SO에 성공적으로 덮어씌워졌습니다.");
    }
    #endif
}

