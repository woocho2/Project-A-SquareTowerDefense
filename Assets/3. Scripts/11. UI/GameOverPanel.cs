using UnityEngine;

namespace SquareTD
{
    // 패배 상태에서 공용 전투 결과 화면 열기
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private BattleResultPanel m_result;
        private void OnEnable() { if (m_result != null) m_result.Open(false); }
    }
}