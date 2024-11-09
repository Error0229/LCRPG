using TMPro;
using UnityEngine;

public class Winner : MonoBehaviour
{
    public TextMeshProUGUI winText;
    [SerializeField] private GameObject cat;
    [SerializeField] private GameObject king;

    public void SetWinner(string winnerName)
    {
        if (winnerName == "PLAYER_A")
        {
            king.SetActive(true);
            winText.text = "Player A Wins";
        }
        else
        {
            cat.SetActive(true);
            winText.text = "Player B Wins";
        }
    }
}