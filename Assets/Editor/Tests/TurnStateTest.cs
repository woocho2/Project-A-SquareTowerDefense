using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TurnStateTest
    {
        [Test]
        public void Idle_아무것도_못함()
        {
            Assert.IsFalse(new IdleState().CanPlayerAct);
        }

        [Test]
        public void PlayerTurn_조작_가능()
        {
            Assert.IsTrue(new PlayerTurnState().CanPlayerAct);
        }

        [Test]
        public void EnemyTurn_조작_못함()
        {
            Assert.IsFalse(new EnemyTurnState().CanPlayerAct);
        }

        [Test]
        public void GameOver_아무것도_못함()
        {
            Assert.IsFalse(new GameOverState().CanPlayerAct);
        }

        [Test]
        public void GameClear_아무것도_못함()
        {
            Assert.IsFalse(new GameClearState().CanPlayerAct);
        }
    }
}
