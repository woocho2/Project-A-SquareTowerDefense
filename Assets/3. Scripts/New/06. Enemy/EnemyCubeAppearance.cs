using UnityEngine;

namespace SquareTD
{
    [CreateAssetMenu(menuName = "SquareTD/Enemy Cube Appearance")]
    public sealed class EnemyCubeAppearance : ScriptableObject
    {
        [SerializeField] private Texture2D m_characterTexture;

        public Texture2D CharacterTexture => m_characterTexture;
    }
}
