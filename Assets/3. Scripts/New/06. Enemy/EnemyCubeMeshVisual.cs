using UnityEngine;

namespace SquareTD
{
    public sealed class EnemyCubeMeshVisual : MonoBehaviour
    {
        public const int FaceCount = 6;
        public const int RollsPerTile = 1;

        [Header("캐릭터 (배경·장식은 공용 재질에서 합성)")]
        [SerializeField] private EnemyCubeAppearance m_appearance;
        [Header("면 에셋 후보 (0 회복 / 1 방어 / 2 행동 / 3 이동 / 4 자물쇠 / 5 모래시계)")]
        [Tooltip("소환할 때 각 면이 독립적으로 뽑습니다. 같은 에셋이 여러 면에 나올 수 있습니다.")]
        [SerializeField] private Material[] m_faceAssetPool;
        [Header("현재 면별 에셋 인덱스 (0 앞 / 1 뒤 / 2 왼쪽 / 3 오른쪽 / 4 위 / 5 아래)")]
        [SerializeField] private int[] m_faceAssetIndices = { 0, 1, 2, 3, 4, 5 };
        [SerializeField, Min(0.01f)] private float m_sideLength = 1f;
        [Header("세 번 굴러 한 타일 이동")]
        [SerializeField, Range(0.25f, 0.6f)] private float m_tileRollDuration = 0.4f;
        [SerializeField] private Material m_shadowMaterial;
        [Header("소환 낙하와 착지")]
        [SerializeField, Min(0.05f)] private float m_spawnFallDuration = 0.34f;
        [SerializeField, Min(0.02f)] private float m_spawnImpactDuration = 0.09f;
        [SerializeField, Min(0.05f)] private float m_spawnBounceDuration = 0.16f;
        [SerializeField, Min(0.1f)] private float m_spawnHeightInSides = 5f;
        [Header("도착 후 정방향 점프")]
        [SerializeField, Min(0.05f)] private float m_uprightHopDuration = 0.4f;
        [SerializeField, Range(0.1f, 0.5f)] private float m_uprightHopHeight = 0.5f;
        [SerializeField, Min(0.02f)] private float m_uprightImpactDuration = 0.07f;
        private Vector3 m_basePosition;
        private Quaternion m_baseRotation;
        private Vector3 m_baseScale;
        private bool m_hasAllFaces;
        private MeshRenderer m_renderer;
        private MaterialPropertyBlock m_appearanceProperties;
        private Material[] m_faceMaterials;
        private MeshRenderer m_shadowRenderer;
        private Vector3 m_shadowScale;
        private MaterialPropertyBlock m_shadowProperties;
        private static Mesh s_shadowMesh;

        public bool HasAllFaces => m_hasAllFaces;
        public float UprightHopDuration => m_uprightHopDuration;
        public float UprightImpactDuration => m_uprightImpactDuration;
        public float TileRollDuration => m_tileRollDuration;
        public float SpawnDuration => m_spawnFallDuration + m_spawnImpactDuration + m_spawnBounceDuration + m_spawnImpactDuration;

        private void Awake()
        {
            m_basePosition = transform.localPosition;
            m_baseRotation = transform.localRotation;
            m_baseScale = transform.localScale;
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            m_renderer = GetComponent<MeshRenderer>();
            ApplyAppearance();
            m_faceMaterials = m_renderer.sharedMaterials;
            if (m_faceAssetPool == null || m_faceAssetPool.Length == 0) m_faceAssetPool = (Material[])m_faceMaterials.Clone();
            if (m_faceAssetIndices == null || m_faceAssetIndices.Length != FaceCount) m_faceAssetIndices = new int[FaceCount];
            m_hasAllFaces = mesh != null && mesh.subMeshCount == FaceCount && m_faceMaterials.Length == FaceCount && m_faceAssetPool.Length == FaceCount;
            foreach (Material material in m_faceMaterials)
            {
                if (material == null) m_hasAllFaces = false;
            }
            foreach (Material material in m_faceAssetPool)
            {
                if (material == null) m_hasAllFaces = false;
            }
            CreateShadow();
        }

        public void SetAppearance(EnemyCubeAppearance appearance)
        {
            m_appearance = appearance;
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            if (m_renderer == null) m_renderer = GetComponent<MeshRenderer>();
            if (m_renderer == null) return;
            if (m_appearanceProperties == null) m_appearanceProperties = new MaterialPropertyBlock();
            int slots = m_renderer.sharedMaterials.Length;
            for (int face = 0; face < slots; face++)
            {
                m_renderer.GetPropertyBlock(m_appearanceProperties, face);
                if (m_appearance != null && m_appearance.CharacterTexture != null)
                {
                    m_appearanceProperties.SetTexture("_MainTex", m_appearance.CharacterTexture);
                    m_appearanceProperties.SetTexture("_EquipmentMask", m_appearance.EquipmentMask != null
                        ? m_appearance.EquipmentMask : Texture2D.blackTexture);
                }
                m_renderer.SetPropertyBlock(m_appearanceProperties, face);
            }
        }

        public void SetSortingOrder(int sortingOrder)
        {
            if (m_renderer == null) m_renderer = GetComponent<MeshRenderer>();
            m_renderer.sortingOrder = sortingOrder;
            if (m_shadowRenderer != null) m_shadowRenderer.sortingOrder = sortingOrder - 1;
        }

        private void CreateShadow()
        {
            if (m_shadowMaterial == null) return;
            if (s_shadowMesh == null)
            {
                s_shadowMesh = new Mesh { name = "EnemyCubeShadowQuad", hideFlags = HideFlags.HideAndDontSave };
                s_shadowMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
                s_shadowMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                s_shadowMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                s_shadowMesh.RecalculateBounds();
            }
            var shadow = new GameObject("CubeGroundShadow");
            shadow.layer = gameObject.layer;
            shadow.transform.SetParent(transform.parent, false);
            shadow.transform.localPosition = new Vector3(m_basePosition.x, m_basePosition.y - m_sideLength * 0.38f, 0.02f);
            shadow.AddComponent<MeshFilter>().sharedMesh = s_shadowMesh;
            m_shadowRenderer = shadow.AddComponent<MeshRenderer>();
            m_shadowRenderer.sharedMaterial = m_shadowMaterial;
            m_shadowRenderer.sortingLayerID = m_renderer.sortingLayerID;
            m_shadowRenderer.sortingOrder = m_renderer.sortingOrder - 1;
            m_shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_shadowRenderer.receiveShadows = false;
            m_shadowScale = new Vector3(m_sideLength * 1.1f, m_sideLength * 0.65f, 1f);
            m_shadowProperties = new MaterialPropertyBlock();
            SetShadow(0f);
        }

        private void SetShadow(float height, float impact = 0f)
        {
            if (m_shadowRenderer == null) return;
            height = Mathf.Clamp01(height);
            m_shadowRenderer.transform.localScale = m_shadowScale * (Mathf.Lerp(1f, 0.65f, height) + impact * 0.8f);
            m_shadowProperties.SetColor("_Color", new Color(0f, 0f, 0f, Mathf.Lerp(0.24f, 0.12f, height)));
            m_shadowRenderer.SetPropertyBlock(m_shadowProperties);
        }

        private void OnDestroy()
        {
            if (m_shadowRenderer != null) Destroy(m_shadowRenderer.gameObject);
        }

        public EnemyCubeOrientation RandomizeForSpawn()
        {
            if (!m_hasAllFaces) return EnemyCubeOrientation.Identity;
            for (int faceIndex = 0; faceIndex < FaceCount; faceIndex++)
            {
                int assetIndex = Random.Range(0, m_faceAssetPool.Length);
                m_faceAssetIndices[faceIndex] = assetIndex;
                m_faceMaterials[faceIndex] = m_faceAssetPool[assetIndex];
            }
            m_renderer.sharedMaterials = m_faceMaterials;
            ApplyAppearance();
            var orientation = EnemyCubeOrientation.FromFace((EnemyCubeFace)Random.Range(0, FaceCount), 0);
            ShowOrientation(orientation);
            return orientation;
        }

        public int GetFaceAssetIndex(int faceIndex)
        {
            if (faceIndex < 0 || faceIndex >= FaceCount) throw new System.ArgumentOutOfRangeException(nameof(faceIndex));
            return m_faceAssetIndices[faceIndex];
        }

        public EnemyCubeFaceAsset GetFaceAsset(EnemyCubeFace face) => (EnemyCubeFaceAsset)GetFaceAssetIndex((int)face);

        public void ShowOrientation(EnemyCubeOrientation orientation)
        {
            transform.localPosition = m_basePosition;
            transform.localRotation = m_baseRotation * RotationFor(orientation);
            transform.localScale = m_baseScale;
            SetShadow(0f);
        }

        public void AnimateSpawn(EnemyCubeOrientation orientation, float elapsed)
        {
            ShowOrientation(orientation);
            elapsed = Mathf.Max(0f, elapsed);
            if (elapsed < m_spawnFallDuration)
            {
                float t = elapsed / m_spawnFallDuration;
                float height = 1f - t * t;
                transform.localPosition += (Vector3.up + Vector3.back * 0.35f) * (m_sideLength * m_spawnHeightInSides * height);
                ApplyScreenSquash(-0.12f * t * t);
                SetShadow(height);
                return;
            }
            elapsed -= m_spawnFallDuration;
            if (elapsed < m_spawnImpactDuration)
            {
                AnimateUprightSquash(orientation, 0.2f * Mathf.Sin(elapsed / m_spawnImpactDuration * Mathf.PI));
                return;
            }
            elapsed -= m_spawnImpactDuration;
            if (elapsed < m_spawnBounceDuration)
            {
                float t = elapsed / m_spawnBounceDuration;
                float hop = 4f * t * (1f - t);
                transform.localPosition += (Vector3.up + Vector3.back * 0.65f) * (m_sideLength * 0.24f * hop);
                ApplyScreenSquash(-0.035f * hop);
                SetShadow(hop * 0.25f);
                return;
            }
            elapsed -= m_spawnBounceDuration;
            if (elapsed < m_spawnImpactDuration)
                AnimateUprightSquash(orientation, 0.075f * Mathf.Sin(elapsed / m_spawnImpactDuration * Mathf.PI));
        }

        public void AnimateTileRoll(EnemyCubeOrientation before, EnemyCubeRollDirection direction, float progress,
            out float travel, out EnemyCubeOrientation committed)
        {
            float phase = Mathf.Clamp01(progress) * RollsPerTile;
            int rollIndex = Mathf.Min((int)phase, RollsPerTile - 1);
            float local = phase - rollIndex;
            committed = before;
            for (int i = 0; i < rollIndex; i++) committed = committed.Rolled(direction);
            const float flightFraction = 0.86f;
            if (local < flightFraction)
            {
                float turn = Mathf.SmoothStep(0f, 1f, local / flightFraction);
                AnimateRoll(committed, direction, turn);
                float bob = Mathf.Sin(turn * Mathf.PI);
                transform.localPosition += Vector3.up * (m_sideLength * 0.045f * bob);
                transform.localScale = m_baseScale * (1f + 0.02f * bob);
                travel = (rollIndex + EnemyCubeRollGeometry.Sample(turn).Travel) / RollsPerTile;
            }
            else
            {
                committed = committed.Rolled(direction);
                float impact = Mathf.Sin((local - flightFraction) / (1f - flightFraction) * Mathf.PI);
                AnimateUprightSquash(committed, 0.045f * impact);
                travel = (rollIndex + 1f) / RollsPerTile;
            }
        }

        public void AnimateUprightHop(EnemyCubeOrientation before, int quarterTurnDirection, float progress)
        {
            progress = Mathf.Clamp01(progress);
            float turn = Mathf.SmoothStep(0f, 1f, progress);
            float hop = 4f * progress * (1f - progress);
            transform.localRotation = m_baseRotation * Quaternion.AngleAxis(quarterTurnDirection * 90f * turn, Vector3.forward) * RotationFor(before);
            transform.localPosition = m_basePosition + (Vector3.back + Vector3.up * 0.65f) * (m_sideLength * m_uprightHopHeight * hop);
            transform.localScale = m_baseScale * (1f + 0.035f * hop);
            SetShadow(hop);
        }

        public void AnimateUprightSquash(EnemyCubeOrientation orientation, float amount)
        {
            ShowOrientation(orientation);
            amount = Mathf.Clamp(amount, 0f, 0.2f);
            ApplyScreenSquash(amount);
            SetShadow(0f, amount);
        }

        private void ApplyScreenSquash(float amount)
        {
            Vector3 localUp = Quaternion.Inverse(transform.localRotation) * Vector3.up;
            float stretch = 1f + amount * 0.5f;
            float compression = 1f - amount;
            Vector3 multipliers = Vector3.one * stretch + new Vector3(Mathf.Abs(localUp.x), Mathf.Abs(localUp.y), Mathf.Abs(localUp.z)) * (compression - stretch);
            transform.localScale = Vector3.Scale(m_baseScale, multipliers);
        }

        public void AnimateRoll(EnemyCubeOrientation before, EnemyCubeRollDirection direction, float progress)
        {
            progress = Mathf.Clamp01(progress);
            transform.localRotation = m_baseRotation * Quaternion.AngleAxis(progress * 90f, RollAxis(direction)) * RotationFor(before);
            float angle = progress * Mathf.PI * 0.5f;
            float lift = m_sideLength * 0.5f * (Mathf.Cos(angle) + Mathf.Sin(angle) - 1f);
            transform.localPosition = m_basePosition + Vector3.back * lift;
            SetShadow(lift / (m_sideLength * 0.34f));
        }

        public static Vector3 RollAxis(EnemyCubeRollDirection direction)
        {
            switch (direction)
            {
                case EnemyCubeRollDirection.Right: return Vector3.down;
                case EnemyCubeRollDirection.Left: return Vector3.up;
                case EnemyCubeRollDirection.Up: return Vector3.right;
                default: return Vector3.left;
            }
        }

        public static Quaternion RotationFor(EnemyCubeOrientation orientation)
        {
            EnemyCubeFace face = orientation.VisibleFace;
            Vector3 normal = FaceNormal(face);
            Vector3 imageUp = FaceImageUp(face);
            Vector3 screenUp = Quaternion.AngleAxis(orientation.CounterClockwiseQuarterTurns * 90f, Vector3.forward) * Vector3.up;
            return Quaternion.LookRotation(Vector3.back, screenUp) * Quaternion.Inverse(Quaternion.LookRotation(normal, imageUp));
        }

        public static Vector3 FaceNormal(EnemyCubeFace face)
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

        public static Vector3 FaceImageUp(EnemyCubeFace face)
        {
            return face == EnemyCubeFace.Top || face == EnemyCubeFace.Bottom ? Vector3.forward : Vector3.up;
        }
    }
}