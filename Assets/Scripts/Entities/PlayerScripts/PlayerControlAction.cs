using SLS.StateMachineV3;
using UnityEngine.InputSystem;
using UltEvents;
using FMOD.Studio;
using EditorAttributes;

public class PlayerControlAction : PlayerStateBehavior
{
    public bool lockAction = true;

    public ActionEntry[] possibleActions;

    [System.Serializable]
    public struct ActionEntry
    {
        public InputActionReference input;
        public UltEvent result;
    }

    public State nextState;

    public override void OnEnter(State prev, bool isFinal)
    {
        if (!isFinal) return;
        controller.currentAction = this;
        if (lockAction) controller.readyForNextAction = false;
    }

    public bool HasAction(InputActionReference button)
    {
        for (int i = 0; i < possibleActions.Length; i++) 
            if (possibleActions[i].input.ToString() == button.ToString()) 
                return true;
        return false;

        //return feedingActions.ContainsKey(button);
    }

    public bool TryNextAction(InputActionReference button)
    {
        for (int i = 0; i < possibleActions.Length; i++)
        {
            if (possibleActions[i].input.ToString() == button.ToString())
            {
                possibleActions[i].result?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void Finish() => nextState.TransitionTo();

    [Button]
    public void MOVE()
    {
        State iState = GetComponent<State>();
        iState.lockReady = lockAction;
        foreach (PlayerControlAction.ActionEntry posAction in possibleActions)
            iState.signals.Add(posAction.input.action.name, posAction.result);
        if (nextState != null)
        {
            iState.signals.Add("Finish", new());
            D next = nextState.TransitionTo;
            iState.signals["Finish"].AddPersistentCall(next);
        }
    }
    private delegate void D();
}