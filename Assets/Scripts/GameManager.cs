using UnityEngine;
using UnityEngine.SceneManagement;



public class GameManager : MonoBehaviour
{
    [Header("Audio")]
    // AudioSource dedicado a la música de fondo (loopea durante toda la partida)
    [SerializeField] private AudioSource musicSource;
    // AudioSource dedicado a los efectos de sonido (los reproduce con PlayOneShot)
    [SerializeField] private AudioSource sfxSource;
    // Sonido que se reproduce cuando el jugador pierde una vida
    [SerializeField] private AudioClip loseLifeSound;
    // Sonido que se reproduce cuando el jugador gana la partida
    [SerializeField] private AudioClip victorySound;
    // Sonido que se reproduce cuando el jugador pierde la partida
    [SerializeField] private AudioClip gameOverSound;

    // Prefab del efecto de victoria que se reproduce al llegar al 85% capturado
    [SerializeField] private GameObject victoryStarsPrefab;

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
    if (_state != GameState.Playing)
    {
        return;
    }

    if (gridManager.GetCapturedPercentage() >= winThreshold)
    {
        // Frenamos la música y reproducimos el sonido de victoria
        musicSource.Stop();
        sfxSource.PlayOneShot(victorySound);

        // Instanciamos el efecto de estrellas en el centro de la grilla
        Instantiate(victoryStarsPrefab, new Vector3(25, 15, 0), Quaternion.identity);

        _state = GameState.Won;
    }
}

        public void StartGame()
    {
        if (_state != GameState.NotStarted)
        {
            return;
        }

        gridManager.GenerateInitialGrid();
        playerController.gameObject.SetActive(true);

        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyController enemy in enemies)
        {
            enemy.gameObject.SetActive(true);
        }

        // Arrancamos la música de fondo (el clip y el loop ya están configurados en el inspector)
        musicSource.Play();

        _state = GameState.Playing;
    }

    // Lo llama el botón Reiniciar: recarga la escena entera
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Lo llama el PlayerController cuando el jugador pierde una vida
    public void HandlePlayerHit()
{
    // Si el jugador ya está en la animación de muerte, ignoramos el hit
    if (playerController.IsDead())
    {
        return;
    }

    _currentLives--;
    gridManager.ResetCurrentTrail();

    if (_currentLives <= 0)
    {
        musicSource.Stop();
        sfxSource.PlayOneShot(gameOverSound);
        _state = GameState.Lost;
        playerController.HandleDeath();
    }
    else
    {
        sfxSource.PlayOneShot(loseLifeSound);
        playerController.HandleDeath();
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