using System;

namespace MutatIO.Flow
{
    public enum GameFlowState
    {
        /// <summary>첫 화면. 입장 전 또는 결과 확인 후.</summary>
        Title,

        /// <summary>월드에서 플레이 중.</summary>
        Playing,

        /// <summary>사망 후 결과 화면.</summary>
        Result,
    }

    /// <summary>
    /// 이번 생의 결과입니다.
    /// 임시: 점수 외 결과 항목(생존 시간·최고 순위·처치 수 등)은 미결정이므로 점수만 담습니다.
    /// </summary>
    public readonly struct PlayResult
    {
        public PlayResult(int score)
        {
            Score = score;
        }

        public int Score { get; }
    }

    /// <summary>
    /// 첫 화면 → 플레이 → 결과 → 첫 화면 상태 전환입니다. 사망 후 자동 부활하지 않고, 결과 확인 뒤 첫 화면에서 다시 입장합니다.
    /// 화면 구성과 월드 조작은 하지 않으며, 허용되지 않은 전환은 false를 반환하고 무시합니다.
    /// </summary>
    public sealed class GameFlow
    {
        public event Action<GameFlowState> StateChanged;

        public GameFlowState State { get; private set; } = GameFlowState.Title;

        /// <summary>마지막으로 끝난 플레이의 결과입니다.</summary>
        public PlayResult LastResult { get; private set; }

        public bool Enter()
        {
            return TryChange(GameFlowState.Title, GameFlowState.Playing);
        }

        public bool EndPlay(PlayResult result)
        {
            if (State != GameFlowState.Playing)
            {
                return false;
            }

            LastResult = result;
            return TryChange(GameFlowState.Playing, GameFlowState.Result);
        }

        public bool ConfirmResult()
        {
            return TryChange(GameFlowState.Result, GameFlowState.Title);
        }

        private bool TryChange(GameFlowState from, GameFlowState to)
        {
            if (State != from)
            {
                return false;
            }

            State = to;
            StateChanged?.Invoke(to);
            return true;
        }
    }
}
