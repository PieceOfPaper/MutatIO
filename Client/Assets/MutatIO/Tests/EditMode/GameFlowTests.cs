using MutatIO.Flow;
using NUnit.Framework;

namespace MutatIO.Tests.EditMode
{
    public sealed class GameFlowTests
    {
        [Test]
        public void Flow_TitlePlayResultTitle()
        {
            var flow = new GameFlow();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Title));

            Assert.That(flow.Enter(), Is.True);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));

            Assert.That(flow.EndPlay(new PlayResult(42f)), Is.True);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Result));
            Assert.That(flow.LastResult.Score, Is.EqualTo(42f));

            Assert.That(flow.ConfirmResult(), Is.True);
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Title));
        }

        [Test]
        public void Flow_IgnoresInvalidTransitions()
        {
            var flow = new GameFlow();
            var changes = 0;
            flow.StateChanged += _ => changes++;

            Assert.That(flow.EndPlay(new PlayResult(1f)), Is.False);
            Assert.That(flow.ConfirmResult(), Is.False);
            flow.Enter();
            Assert.That(flow.Enter(), Is.False);
            flow.EndPlay(new PlayResult(5f));
            Assert.That(flow.EndPlay(new PlayResult(9f)), Is.False);

            Assert.That(flow.State, Is.EqualTo(GameFlowState.Result));
            Assert.That(flow.LastResult.Score, Is.EqualTo(5f));
            Assert.That(changes, Is.EqualTo(2));
        }
    }
}
