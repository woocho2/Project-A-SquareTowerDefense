using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class SynergySkillButtonTest
    {
        private GameObject m_root;
        private BoardManager m_board;
        private TurnManager m_turn;
        private BoardInputController m_input;
        private SynergyBoard m_synergy;
        private static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);

        private T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(m_root.transform);
            return go.AddComponent<T>();
        }

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("SynergySkillButtonTest");
            m_board = Child<BoardManager>("Board");
            m_turn = Child<TurnManager>("Turns");
            m_input = Child<BoardInputController>("Input");
            typeof(Singleton<BoardManager>).GetProperty("Instance").SetValue(null, m_board);
            typeof(Singleton<TurnManager>).GetProperty("Instance").SetValue(null, m_turn);
            var grid = new BoardGrid(new[] { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0) },
                new[] { new Vector3Int(0, 1, 0) });
            Set(m_board, "Grid", grid);
            Set(m_board, "Slots", new TowerSlots(1));
            Set(m_board, "Occupancy", new PathOccupancy());
            Set(m_turn, "Current", new PlayerTurnState());
            m_synergy = new SynergyBoard(m_board);
            EmblemSharedState.ResetOutsideGame();
        }

        [TearDown]
        public void TearDown()
        {
            m_input.Current?.Exit();
            typeof(Singleton<BoardManager>).GetProperty("Instance").SetValue(null, null);
            typeof(Singleton<TurnManager>).GetProperty("Instance").SetValue(null, null);
            Object.DestroyImmediate(m_root);
            EmblemSharedState.ResetOutsideGame();
        }

        private SynergyBoard.Entry Entry(int id)
        {
            SynergyRecipes.TryGet(id, out var recipe);
            var entry = new SynergyBoard.Entry { Recipe = recipe, Skill = SynergyEffects.Create(id) };
            entry.Skill.Fill();
            return entry;
        }

        private PickSynergyTargetMode Pick(SynergyBoard.Entry entry)
        {
            var mode = new PickSynergyTargetMode(m_input, m_synergy, entry);
            m_input.ChangeMode(mode);
            return mode;
        }

        private static BoardPoint Path(int index) => new BoardPoint(Vector3.zero, index, -1);

        [Test]
        public void 충전_전이나_일시정지_종료_상태에서는_버튼_비활성()
        {
            var skill = new BrynhildSkill();
            Assert.IsFalse(SynergySkillButton.CanActivate(skill, new PlayerTurnState(), false));
            skill.Fill();
            Assert.IsFalse(SynergySkillButton.CanActivate(skill, new PlayerTurnState(), true));
            Assert.IsFalse(SynergySkillButton.CanActivate(skill, new GameOverState(), false));
            Assert.IsFalse(SynergySkillButton.CanActivate(skill, new GameClearState(), false));
            Assert.IsFalse(SynergySkillButton.CanActivate(skill, new IdleState(), false));
        }

        [Test]
        public void 충전된_일반_시너지_스킬은_플레이어턴과_적턴에_사용_가능()
        {
            var entry = Entry(SynergyEffects.BrynhildId);
            Assert.IsTrue(SynergySkillButton.CanActivate(entry.Skill, new PlayerTurnState(), false));
            Assert.IsTrue(SynergySkillButton.CanActivate(entry.Skill, new EnemyTurnState(), false));
        }

        [TestCase(SynergyEffects.LokiId)]
        [TestCase(SynergyEffects.NjordId)]
        public void 길_선택_스킬은_플레이어턴만(int id)
        {
            var entry = Entry(id);
            Assert.IsTrue(SynergySkillButton.CanActivate(entry.Skill, new PlayerTurnState(), false));
            Assert.IsFalse(SynergySkillButton.CanActivate(entry.Skill, new EnemyTurnState(), false));
        }

        [Test]
        public void 뇨르드는_길_한칸_선택_뒤_스택을_소모하고_선택_종료()
        {
            var entry = Entry(SynergyEffects.NjordId);
            Pick(entry).OnPointerDown(Path(1));
            Assert.IsTrue(EmblemSharedState.Current.IsStopTile(1));
            Assert.AreEqual(1, entry.Uses);
            Assert.AreEqual(0f, entry.Skill.Stack.Stacks);
            Assert.IsInstanceOf<IdleMode>(m_input.Current);
        }

        [Test]
        public void 로키는_서로_다른_두칸을_선택해야_소모()
        {
            var entry = Entry(SynergyEffects.LokiId);
            var mode = Pick(entry);
            mode.OnPointerDown(Path(0));
            mode.OnPointerDown(Path(0));
            Assert.IsTrue(entry.Skill.IsReady);
            Assert.AreEqual(0, entry.Uses);
            mode.OnPointerDown(Path(2));
            Assert.AreEqual(1, entry.Uses);
            Assert.IsFalse(entry.Skill.IsReady);
            Assert.IsInstanceOf<IdleMode>(m_input.Current);
        }

        [Test]
        public void 선택_취소나_길밖_클릭은_스택을_소모하지_않음()
        {
            var entry = Entry(SynergyEffects.LokiId);
            var mode = Pick(entry);
            mode.OnPointerDown(Path(0));
            mode.OnCancel();
            Assert.IsTrue(entry.Skill.IsReady);
            mode = Pick(entry);
            mode.OnPointerDown(new BoardPoint(Vector3.zero, -1, 0));
            Assert.IsTrue(entry.Skill.IsReady);
            Assert.AreEqual(0, entry.Uses);
            Assert.IsInstanceOf<IdleMode>(m_input.Current);
        }

        [Test]
        public void 선택_도중_적턴이_되면_사용하지_않음()
        {
            var entry = Entry(SynergyEffects.LokiId);
            var mode = Pick(entry);
            mode.OnPointerDown(Path(0));
            Set(m_turn, "Current", new EnemyTurnState());
            mode.OnPointerDown(Path(1));
            Assert.IsTrue(entry.Skill.IsReady);
            Assert.AreEqual(0, entry.Uses);
            Assert.IsInstanceOf<IdleMode>(m_input.Current);
        }

        [Test]
        public void 발드르는_스택이_비어도_보관한_횟수가_있으면_사용_가능()
        {
            var entry = Entry(SynergyEffects.BaldrId);
            Assert.AreEqual(0f, entry.Skill.Stack.Ratio);
            Assert.IsTrue(SynergySkillButton.CanActivate(entry.Skill, new EnemyTurnState(), false));
        }

        [Test]
        public void 공용_타워에_버튼_프리팹이_연결되고_충전_비율이_표시됨()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/1. Tower.prefab");
            var binding = prefab.GetComponent<SynergySkillButton>();
            Assert.IsNotNull(binding);
            var viewPrefab = new SerializedObject(binding).FindProperty("m_viewPrefab").objectReferenceValue as SynergySkillButtonView;
            Assert.IsNotNull(viewPrefab);
            var view = Object.Instantiate(viewPrefab, m_root.transform);
            view.Render(0.4f, false, false, false, "40%");
            Assert.AreEqual(0.4f, view.FillAmount, 0.001f);
            Assert.AreEqual("40%", view.Label);
            Assert.IsFalse(view.ActionButton.interactable);
            view.Render(1f, true, true, false, "스킬 사용");
            Assert.IsTrue(view.ActionButton.interactable);
            Assert.AreEqual(1f, view.FillAmount);
            var fill = new SerializedObject(view).FindProperty("m_fill").objectReferenceValue as UnityEngine.UI.Image;
            Assert.IsNotNull(fill.sprite, "Filled Image에는 스프라이트가 있어야 실제로 채워짐");
        }
    }
}
