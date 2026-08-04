namespace RageRooster.Core
{
    public interface IPlayerStateMachine
    {
        void CutsceneState();
        void UnCutsceneState();
        bool SendSignal(string signal);
    }
}
