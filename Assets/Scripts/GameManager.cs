using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Cantidad de vidas con las que arranca el jugador
    public int startingLives = 3;

    // Porcentaje de área que hay que capturar para ganar (entre 0 y 1)
    public float winThreshold = 0.85f;

    // Referencia al GridManager para consultar el porcentaje y reiniciar el rastro
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
        NotStarted,
        Playing,
        Won,
        Lost
    }

    private void Start()
    {
        // Al iniciar arrancamos con las vidas configuradas y en estado "no iniciado"
        _currentLives = startingLives;
        _state = GameState.NotStarted;
    }

    private void Update()
    {
        // Solo chequeamos victoria si estamos jugando
        if (_state != GameState.Playing)
        {
            return;
        }

        // Consultamos el porcentaje a la grilla
        float progress = gridManager.GetCapturedPercentage();

        // Si superamos el umbral, ganamos
        if (progress >= winThreshold)
        {
            OnVictory();
        }
    }

        // Lo llama el botón Play: genera la grilla, activa jugador y enemigos, arranca la partida
    public void StartGame()
    {
        if (_state != GameState.NotStarted)
        {
            return;
        }

        // Generamos visualmente el borde de la grilla
        gridManager.GenerateInitialGrid();

        // Activamos al jugador (estaba desactivado en la escena)
        playerController.gameObject.SetActive(true);

        // Buscamos todos los enemigos (incluyendo los desactivados) y los activamos
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyController enemy in enemies)
        {
            enemy.gameObject.SetActive(true);
        }

        // Cambiamos el estado a Playing para que el resto del sistema se mueva
        _state = GameState.Playing;
    }

    // Lo llama el botón Reiniciar: recarga la escena entera
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Método público que los enemigos llaman cuando golpean al jugador o al rastro
    public void HandlePlayerHit()
    {
        if (_state != GameState.Playing)
        {
            return;
        }

        _currentLives--;
        Debug.Log("Vida perdida. Vidas restantes: " + _currentLives);

        gridManager.ResetCurrentTrail();
        playerController.ResetToStart();

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

    // Indica si todavía no se inició la partida
    public bool HasNotStarted()
    {
        return _state == GameState.NotStarted;
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