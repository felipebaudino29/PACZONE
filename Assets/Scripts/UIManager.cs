using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Referencia al GameManager para leer los datos del juego
    public GameManager gameManager;

    // Texto que muestra las vidas restantes
    public TMP_Text livesText;

    // Texto que muestra el porcentaje de progreso
    public TMP_Text progressText;

    // Texto que muestra mensajes centrales (título, victoria, derrota)
    public TMP_Text messageText;

    // Botón para arrancar la partida
    public Button playButton;

    // Botón para reiniciar la partida al terminar
    public Button restartButton;

    private void Start()
    {
        // Conectamos cada botón a la acción que debe ejecutar al cliquearse
        playButton.onClick.AddListener(OnPlayClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void Update()
    {
        // Actualizamos los textos de vidas y progreso siempre
        livesText.text = "Lives: " + gameManager.GetCurrentLives();
        int progressPercent = Mathf.RoundToInt(gameManager.GetCurrentProgress() * 100f);
        progressText.text = "Progress: " + progressPercent + "%";

        // Cambiamos el mensaje central y la visibilidad de los botones según el estado
        if (gameManager.HasNotStarted())
        {
            // Cada letra de PAC-EATER tiene un color distinto (paleta arcade clásica)
            messageText.text = "<color=#FFD700>P</color><color=#FF1B1B>A</color><color=#FF69B4>C</color><color=#FFFFFF>-</color><color=#00FFFF>E</color><color=#FFA500>A</color><color=#FFD700>T</color><color=#FF69B4>E</color><color=#00FFFF>R</color>";
            // El color base lo dejamos blanco para que no tiña los colores de cada letra
            messageText.color = Color.white;
            playButton.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(false);
        }
        else if (gameManager.HasWon())
        {
            messageText.text = "VICTORY";
            messageText.color = new Color(1f, 0.84f, 0f); // amarillo arcade
            playButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(true);
        }
        else if (gameManager.HasLost())
        {
            messageText.text = "GAME OVER";
            messageText.color = new Color(0.88f, 0f, 0f); // rojo
            playButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(true);
        }
        else
        {
            // Estado Playing: limpiamos el mensaje y ocultamos los botones
            messageText.text = "";
            playButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
        }
    }

    // Se ejecuta cuando el jugador clickea Play
    private void OnPlayClicked()
    {
        gameManager.StartGame();
    }

    // Se ejecuta cuando el jugador clickea Reiniciar
    private void OnRestartClicked()
    {
        gameManager.RestartGame();
    }
}