using TMPro;
using UnityEngine;

// If using TextMeshPro

public class GameStatText : MonoBehaviour
{
    public TextMeshProUGUI text;

    public void SetInfo(int round, int AWins, int BWins, int match, string who, Role role)
    {
        // A Wins:1, B Wins:2
        // Match: 2 Round: 2
        // PLAYER_A's turn, as the Attacker
        text.SetText(
            $"A Wins:{AWins}, " + $"B Wins:{BWins}\n" +
            $"Match: {match}\n" + $"Round: {round}\n" +
            $"{who}'s turn, as the {role}"
        );
    }
}