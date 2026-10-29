using UnityEngine;

namespace SquareTD
{
    // 적 큐브 겉모습 설정 (캐릭터 그림)
    [CreateAssetMenu(menuName = "SquareTD/Enemy Cube Appearance")]
    public sealed class EnemyCubeAppearance : ScriptableObject
    {
        [SerializeField] private Texture2D m_characterTexture;
        [SerializeField] private Sprite m_faceSprite;
        [SerializeField] private Sprite m_fullSprite;

        public Sprite FaceSprite => m_faceSprite;
        public Sprite FullSprite => m_fullSprite;
        public Texture2D CharacterTexture => m_faceSprite != null ? m_faceSprite.texture : m_characterTexture;

        // Read the current sprite rectangle so later Sprite Editor adjustments are used automatically.
        public Rect CharacterUvRect
        {
            get
            {
                if (m_faceSprite == null) return new Rect(0f, 0f, 1f, 1f);
                Texture2D texture = m_faceSprite.texture;
                Rect rect = m_faceSprite.rect;
                return new Rect(rect.x / texture.width, rect.y / texture.height,
                    rect.width / texture.width, rect.height / texture.height);
            }
        }
    }
}
