using TMPro;
using UnityEngine;

public class WinConditionWindow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI winText;

    public void SetWinner(bool isWinner)
    {
        winText.text = isWinner ? "YOU WIN!!!" : "YOU LOSE!";
    }
}
