using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Welcome : IState
{
    private readonly InputAction _submit;

    public Welcome(GameStateMachine machine) : base(machine)
    {
        _submit = InputSystem.actions.FindAction("Submit");
    }

    public override void OnEnter()
    {
    }

    public override async void OnUpdate()
    {
        if (_submit.WasReleasedThisFrame())
        {
            await SceneManager.LoadSceneAsync("InGame");
            Machine.ChangeState(new InGame(Machine));
        }
    }

    public override void OnExit()
    {
    }
}