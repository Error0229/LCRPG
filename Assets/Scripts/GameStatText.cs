using TMPro;
using UnityEngine;

// If using TextMeshPro

public class GameStatText : MonoBehaviour
{
    public TextMeshProUGUI text;


    private void Start()
    {
        // Start the animation sequence
    }

    private void Update()
    {
    }

    public void SetInfo(int round, int AWins, int BWins, int match)
    {
        text.SetText(
            $"Round: {round}\n" +
            $"A Wins: {AWins}\n" +
            $"B Wins: {BWins}\n" +
            $"Match: {match}"
        );
    }
}