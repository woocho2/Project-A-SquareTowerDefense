using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TurnStateTest
    {
        [Test]
        public void Idle_아무것도_못함()
        {
            ITurnState state = new IdleState();
            Assert.IsFalse(state.CanPlayerAct);
            Assert.IsFalse(state.CanConsumeStack);
        }

        [Test]
        public void PlayerTurn_조작과_스택소모_가능()
        {
            ITurnState state = new PlayerTurnState();
            Assert.IsTrue(state.CanPlayerAct);
            Assert.IsTrue(state.CanConsumeStack);
        }

        [Test]
        public void EnemyTurn_조작은_못하고_스택소모는_가능()
        {
            ITurnState state = new EnemyTurnState();
            Assert.IsFalse(state.CanPlayerAct);
            Assert.IsTrue(state.CanConsumeStack);
        }

        [Test]
        public void GameOver_아무것도_못함()
        {
            ITurnState state = new GameOverState();
            Assert.IsFalse(state.CanPlayerAct);
            Assert.IsFalse(state.CanConsumeStack);
        }

        [Test]
        public void GameClear_아무것도_못함()
        {
            ITurnState state = new GameClearState();
            Assert.IsFalse(state.CanPlayerAct);
            Assert.IsFalse(state.CanConsumeStack);
        }
    }
}
