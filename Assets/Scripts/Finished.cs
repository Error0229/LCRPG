using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Finished : IState
{
    private readonly InputAction _submit;
    private Winner _winner;

    public Finished(GameStateMachine machine) : base(machine)
    {
        _submit = InputSystem.actions.FindAction("Submit");
    }

    public override void OnEnter()
    {
        _winner = GameObject.Find("Winner").GetComponent<Winner>();
        var winner = GameStateMachine.Instance.WhoWins;
        _winner.SetWinner(winner);
    }

    public override void OnUpdate()
    {
        if (_submit.WasReleasedThisFrame())
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadSceneAsync("Welcome");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Machine.ChangeState(new Welcome(Machine));
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnExit()
    {
    }
}