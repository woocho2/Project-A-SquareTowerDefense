using UnityEngine;

namespace SquareTD
{
    // 카드·존 고리가 쓰는 타워 아이콘 표 (문양 아이콘, 티어 배지, 컬러 색)
    [CreateAssetMenu(menuName = "SquareTD/Tower Icons")]
    public class TowerIcons : ScriptableObject
    {
        [Tooltip("검, 도끼, 활, 창, 방패, 해머, 불, 물, 대지, 바람, 얼음, 전기, 빛, 어둠 순서")]
        [SerializeField] private Sprite[] m_emblems;
        [Tooltip("1 브론즈 ~ 5 다이아")]
        [SerializeField] private Sprite[] m_tiers;
        [SerializeField] private Color m_splashColor = new Color32(226, 93, 98, 255);
        [SerializeField] private Color m_targetColor = new Color32(83, 138, 205, 255);
        [SerializeField] private Color m_buffColor = new Color32(213, 173, 40, 255);
        [SerializeField] private Color m_debuffColor = new Color32(32, 32, 39, 255);

        private static TowerIcons s_instance;

        public static TowerIcons Instance
        {
            get
            {
                if (s_instance == null) s_instance = Resources.Load<TowerIcons>("TowerIcons");
                return s_instance;
            }
        }

        public Sprite GetEmblem(TowerEmblem emblem) => Get(m_emblems, (int)emblem);

        public Sprite GetTier(int tier) => Get(m_tiers, tier);

        public Color GetColor(TowerColor color) => color switch
        {
            TowerColor.Red => m_splashColor,
            TowerColor.Blue => m_targetColor,
            TowerColor.Yellow => m_buffColor,
            TowerColor.Purple => m_debuffColor,
            _ => Color.white
        };

        private static Sprite Get(Sprite[] sprites, int number)
        {
            int index = number - 1;
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
