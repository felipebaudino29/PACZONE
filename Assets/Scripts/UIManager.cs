using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Referencia al GameManager para leer los datos del juego
    public GameManager gameManager;

    // Texto que muestra las vidas restantes
    public TMP_Text livesText;

    // Texto que muestra el porcentaje de progreso
    public TMP_Text progressText;

    // Texto que muestra el mensaje de victoria o derrota
    public TMP_Text messageText;

    private void Update()
    {
        // Actualizamos el texto de vidas
        livesText.text = "Vidas: " + gameManager.GetCurrentLives();

        // Actualizamos el texto de progreso (convertimos de 0-1 a 0-100 y redondeamos)
        int progressPercent = Mathf.RoundToInt(gameManager.GetCurrentProgress() * 100f);
        progressText.text = "Progreso: " + progressPercent + "%";

        // Mostramos el mensaje correspondiente según el estado del juego
        if (gameManager.HasWon())
        {
            messageText.text = "VICTORIA";
        }
        else if (gameManager.HasLost())
        {
            messageText.text = "GAME OVER";
        }
        else
        {
            // Durante el juego no mostramos ningún mensaje
            messageText.text = "";
        }
    }
}