using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Cantidad de vidas con las que arranca el jugador
    public int startingLives = 3;

    // Porcentaje de área que hay que capturar para ganar (entre 0 y 1)
    public float winThreshold = 0.85f;

    // Referencia al GridManager para consultar el porcentaje capturado y reiniciar el rastro
    public GridManager gridManager;

    // Referencia al PlayerController para reposicionarlo cuando pierde una vida
    public PlayerController playerController;

    // Vidas actuales del jugador
    private int _currentLives;

    // Estado actual del juego
    private GameState _state;

    // Enumeración con los posibles estados del juego
    private enum GameState
    {
        Playing,
        Won,
        Lost
    }

    private void Start()
    {
        // Al iniciar, las vidas son las del valor configurado y el estado es "jugando"
        _currentLives = startingLives;
        _state = GameState.Playing;
    }

    private void Update()
    {
        // Si el juego ya terminó, no hacemos más controles
        if (_state != GameState.Playing)
        {
            return;
        }

        // Le preguntamos a la grilla el porcentaje actual capturado
        float progress = gridManager.GetCapturedPercentage();

        // Si llegamos o superamos el umbral, ganamos
        if (progress >= winThreshold)
        {
            OnVictory();
        }
    }

    // Método público que los enemigos llamarán cuando golpeen al jugador o al rastro
    public void HandlePlayerHit()
    {
        // Si el juego ya terminó, ignoramos
        if (_state != GameState.Playing)
        {
            return;
        }

        // Restamos una vida
        _currentLives--;

        // Log para verificar el funcionamiento (después lo reemplazamos por UI)
        Debug.Log("Vida perdida. Vidas restantes: " + _currentLives);

        // Borramos el rastro pendiente del mapa
        gridManager.ResetCurrentTrail();

        // Reposicionamos al jugador en su punto de inicio
        playerController.ResetToStart();

        // Si no quedan vidas, se perdió el juego
        if (_currentLives <= 0)
        {
            OnDefeat();
        }
    }

    // Devuelve las vidas actuales (lo va a usar la UI)
    public int GetCurrentLives()
    {
        return _currentLives;
    }

    // Devuelve el progreso actual entre 0 y 1 (lo va a usar la UI)
    public float GetCurrentProgress()
    {
        return gridManager.GetCapturedPercentage();
    }

    // Indica si el juego todavía está en curso
    public bool IsPlaying()
    {
        return _state == GameState.Playing;
    }

        // Indica si el jugador ganó la partida
    public bool HasWon()
    {
        return _state == GameState.Won;
    }

    // Indica si el jugador perdió la partida
    public bool HasLost()
    {
        return _state == GameState.Lost;
    }

    // Se ejecuta cuando se alcanza el porcentaje de victoria
    private void OnVictory()
    {
        _state = GameState.Won;
        Debug.Log("VICTORIA");
    }

    // Se ejecuta cuando se quedan sin vidas
    private void OnDefeat()
    {
        _state = GameState.Lost;
        Debug.Log("DERROTA");
    }
}