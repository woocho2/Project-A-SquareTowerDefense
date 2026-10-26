using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    public enum EnemyCubeFace { Front = 0, Back = 1, Left = 2, Right = 3, Top = 4, Bottom = 5 }
    public enum EnemyCubeFaceAsset { SelfHeal = 0, Defense = 1, Action = 2, Mobility = 3, Locked = 4, Hourglass = 5 }

    public sealed class EnemyCubeMeshVisual : MonoBehaviour
    {
        public const int FaceCount = 6;

        [SerializeField] private EnemyCubeAppearance m_appearance;
        [SerializeField] private Material[] m_faceAssetPool;
        [SerializeField] private float m_spawnHeight = 5f;
        [SerializeField] private float m_spawnFallDuration = 0.34f;
        [SerializeField] private float m_uprightHopDuration = 0.4f;
        [SerializeField] private float m_uprightHopPower = 0.3f;

        private readonly int[] m_faceAssetIndices = new int[FaceCount];
        private Material[] m_faceMaterials;
        private MeshRenderer m_renderer;
        private MaterialPropertyBlock m_appearanceProperties;
        private Vector3 m_basePosition;

        public bool HasAllFaces { get; private set; }

        private void Awake()
        {
            m_basePosition = transform.localPosition;
            m_renderer = GetComponent<MeshRenderer>();
            m_appearanceProperties = new MaterialPropertyBlock();
            m_faceMaterials = m_renderer.sharedMaterials;

            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            HasAllFaces = mesh != null && mesh.subMeshCount == FaceCount
                && m_faceMaterials.Length == FaceCount && m_faceAssetPool.Length == FaceCount;
            foreach (Material material in m_faceAssetPool)
            {
                if (material == null) HasAllFaces = false;
            }

            ApplyAppearance();
        }

        public void SetAppearance(EnemyCubeAppearance appearance)
        {
            m_appearance = appearance;
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (m_appearance == null || m_appearance.CharacterTexture == null) return;

            for (int face = 0; face < m_faceMaterials.Length; face++)
            {
                m_renderer.GetPropertyBlock(m_appearanceProperties, face);
                m_appearanceProperties.SetTexture("_MainTex", m_appearance.CharacterTexture);
                m_renderer.SetPropertyBlock(m_appearanceProperties, face);
            }
        }

        public void SetSortingOrder(int sortingOrder) => m_renderer.sortingOrder = sortingOrder;

        public void RandomizeForSpawn()
        {
            transform.localPosition = m_basePosition;

            if (HasAllFaces)
            {
                for (int face = 0; face < FaceCount; face++)
                {
                    int assetIndex = Random.Range(0, m_faceAssetPool.Length);
                    m_faceAssetIndices[face] = assetIndex;
                    m_faceMaterials[face] = m_faceAssetPool[assetIndex];
                }
                m_renderer.sharedMaterials = m_faceMaterials;
                ApplyAppearance();
            }

            ShowFace((EnemyCubeFace)Random.Range(0, FaceCount));
        }

        public int GetFaceAssetIndex(int faceIndex) => m_faceAssetIndices[faceIndex];

        public EnemyCubeFaceAsset GetFaceAsset(EnemyCubeFace face) => (EnemyCubeFaceAsset)m_faceAssetIndices[(int)face];

        public EnemyCubeFace VisibleFace
        {
            get
            {
                EnemyCubeFace best = EnemyCubeFace.Front;
                float bestDot = float.MinValue;
                for (int i = 0; i < FaceCount; i++)
                {
                    EnemyCubeFace face = (EnemyCubeFace)i;
                    float dot = Vector3.Dot(transform.rotation * FaceNormal(face), Vector3.back);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        best = face;
                    }
                }
                return best;
            }
        }

        public Tween Roll(Vector3 direction, float duration)
        {
            Vector3 axis = Vector3.Cross(Vector3.back, direction);
            return transform.DORotate(axis * 90f, duration, RotateMode.WorldAxisAdd).SetEase(Ease.InOutSine);
        }

        public Tween StandUpright()
        {
            Vector3 imageUp = transform.rotation * FaceImageUp(VisibleFace);
            float angle = Mathf.Round(Vector3.SignedAngle(imageUp, Vector3.up, Vector3.forward) / 90f) * 90f;
            if (angle == 0f) return null;

            int hops = Mathf.RoundToInt(Mathf.Abs(angle) / 90f);
            float step = Mathf.Sign(angle) * 90f;

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < hops; i++)
            {
                sequence.Append(transform.DORotate(new Vector3(0f, 0f, step), m_uprightHopDuration, RotateMode.WorldAxisAdd));
                sequence.Join(transform.DOLocalJump(m_basePosition, m_uprightHopPower, 1, m_uprightHopDuration));
            }
            return sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public Sequence PlaySpawn()
        {
            transform.localPosition = m_basePosition + Vector3.up * m_spawnHeight;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOLocalMove(m_basePosition, m_spawnFallDuration).SetEase(Ease.InQuad));
            sequence.Append(transform.DOLocalJump(m_basePosition, 0.24f, 1, 0.16f));
            return sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void SnapRotation()
        {
            transform.rotation = Quaternion.LookRotation(RoundToAxis(transform.forward), RoundToAxis(transform.up));
        }

        private void ShowFace(EnemyCubeFace face)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up)
                * Quaternion.Inverse(Quaternion.LookRotation(FaceNormal(face), FaceImageUp(face)));
        }

        private static Vector3 RoundToAxis(Vector3 v) => new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));

        private static Vector3 FaceNormal(EnemyCubeFace face)
        {
            switch (face)
            {
                case EnemyCubeFace.Back: return Vector3.forward;
                case EnemyCubeFace.Left: return Vector3.left;
                case EnemyCubeFace.Right: return Vector3.right;
                case EnemyCubeFace.Top: return Vector3.up;
                case EnemyCubeFace.Bottom: return Vector3.down;
                default: return Vector3.back;
            }
        }

        private static Vector3 FaceImageUp(EnemyCubeFace face)
        {
            return face == EnemyCubeFace.Top || face == EnemyCubeFace.Bottom ? Vector3.forward : Vector3.up;
        }
    }
}
