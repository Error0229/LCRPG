using UnityEngine;
using UnityEngine.Events;

public class GameStateMachine : MonoBehaviour
{
    public UnityEvent<string> OnStateChanged;
    private IState _currentState;
    public static GameStateMachine Instance { get; private set; }

    public string CurrentStateName => _currentState?.GetType().Name;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensures only one instance exists
            return;
        }

        Instance = this;
        OnStateChanged = new UnityEvent<string>();
        ChangeState(new Welcome(this));
        DontDestroyOnLoad(gameObject); // Prevents destruction when changing scenes
    }

    private void Update()
    {
        _currentState?.OnUpdate();
    }

    public void ChangeState(IState newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState?.OnEnter();
        OnStateChanged?.Invoke(_currentState?.GetType().Name);
    }
}