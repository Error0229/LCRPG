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

    public override void OnUpdate()
    {
        if (_submit.WasReleasedThisFrame())
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadSceneAsync("InGame");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Machine.ChangeState(new InGame(Machine));
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnExit()
    {
    }
}