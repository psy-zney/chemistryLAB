using System;

namespace ChemistryLab.Presentation
{
    /// <summary>High-level application states. Gameplay is available only in MainLab.</summary>
    public enum AppState
    {
        Boot = 0,
        Loading = 1,
        MainLab = 2,
        Modal = 3,
        Paused = 4,
        ErrorRecovery = 5
    }

    /// <summary>
    /// Small, framework-free state machine used by the bootstrapper and navigation
    /// adapters. Invalid transitions are rejected instead of silently changing state.
    /// </summary>
    public sealed class AppStateController
    {
        public AppStateController()
            : this(AppState.Boot)
        {
        }

        public AppStateController(AppState initialState)
        {
            State = initialState;
        }

        public AppState State { get; private set; }
        public bool IsGameplayAvailable { get { return State == AppState.MainLab; } }

        public event Action<AppState, AppState> StateChanged;

        public bool TransitionTo(AppState nextState)
        {
            if (nextState == State)
            {
                return true;
            }

            if (!CanTransition(State, nextState))
            {
                return false;
            }

            var previousState = State;
            State = nextState;
            var handler = StateChanged;
            if (handler != null)
            {
                handler(previousState, nextState);
            }

            return true;
        }

        public static bool CanTransition(AppState from, AppState to)
        {
            switch (from)
            {
                case AppState.Boot:
                    return to == AppState.Loading || to == AppState.ErrorRecovery;
                case AppState.Loading:
                    return to == AppState.MainLab || to == AppState.ErrorRecovery;
                case AppState.MainLab:
                    return to == AppState.Modal || to == AppState.Paused || to == AppState.ErrorRecovery;
                case AppState.Modal:
                    return to == AppState.MainLab || to == AppState.Paused || to == AppState.ErrorRecovery;
                case AppState.Paused:
                    return to == AppState.MainLab || to == AppState.Modal || to == AppState.ErrorRecovery;
                case AppState.ErrorRecovery:
                    return to == AppState.Loading;
                default:
                    return false;
            }
        }
    }
}
