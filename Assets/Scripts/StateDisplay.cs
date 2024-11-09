using TMPro;
using UnityEngine;

public class StateDisplay : MonoBehaviour
{
    public TextMeshProUGUI stateText; // Assign this in the inspector

    private void Start()
    {
        if (GameStateMachine.Instance != null)
        {
            // Subscribe to the state change event
            GameStateMachine.Instance.OnStateChanged.AddListener(UpdateStateText);
            UpdateStateText(GameStateMachine.Instance.CurrentStateName);
        }
    }

    private void Update()
    {
        // if press tab hide the text
        if (Input.GetKeyDown(KeyCode.Tab)) stateText.gameObject.SetActive(!stateText.gameObject.activeSelf);
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (GameStateMachine.Instance != null) GameStateMachine.Instance.OnStateChanged.RemoveListener(UpdateStateText);
    }

    private void UpdateStateText(string stateName)
    {
        stateText.text = "Current State: " + stateName;
    }
}