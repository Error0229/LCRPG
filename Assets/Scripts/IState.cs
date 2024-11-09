public abstract class IState
{
    protected GameStateMachine Machine;

    public IState(GameStateMachine machine)
    {
        Machine = machine;
    }


    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}