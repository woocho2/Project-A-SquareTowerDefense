using UnityEngine;

namespace SquareTD
{
    [CreateAssetMenu(menuName = "SquareTD/Enemy Cube Appearance")]
    public sealed class EnemyCubeAppearance : ScriptableObject
    {
        [SerializeField] private Texture2D m_characterTexture;
        [Tooltip("흰색 영역만 상태별 장비 색상으로 변경합니다. 피부·눈·머리 영역은 검은색입니다.")]
        [SerializeField] private Texture2D m_equipmentMask;

        public Texture2D CharacterTexture => m_characterTexture;
        public Texture2D EquipmentMask => m_equipmentMask;
    }
}
