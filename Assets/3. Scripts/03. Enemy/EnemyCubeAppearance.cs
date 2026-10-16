using UnityEngine;

/// <summary>큐브의 공용 배경/장식과 별개로 교체할 캐릭터와 장비 색상 마스크입니다.</summary>
[CreateAssetMenu(menuName = "SquareTowerDefense/Enemy Cube Appearance")]
public sealed class EnemyCubeAppearance : ScriptableObject
{
    [SerializeField] private Texture2D m_characterTexture;
    [Tooltip("흰색 영역만 상태별 장비 색상으로 변경합니다. 피부·눈·머리 영역은 검은색입니다.")]
    [SerializeField] private Texture2D m_equipmentMask;

    public Texture2D CharacterTexture => m_characterTexture;
    public Texture2D EquipmentMask => m_equipmentMask;
}
