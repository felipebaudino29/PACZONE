using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // Duración (en segundos) de la animación de muerte antes de respawnear
    [SerializeField] private float deathAnimationDuration = 0.5f;

    // Indica si el jugador está actualmente "muriendo" (no se mueve, no recibe más golpes)
    private bool _isDead = false;
    // Velocidad a la que el personaje se mueve entre celdas
    public float moveSpeed = 5f;

    // Referencia pública a nuestro GridManager para poder hacerle consultas
    public GridManager gridManager;

    // Referencia al GameManager para saber si el juego está activo y avisar colisiones
    public GameManager gameManager;

    // Posición de inicio del personaje (se guarda al arrancar)
    private Vector2 _startPosition;

    // La posición exacta de la celda a la que nos estamos moviendo
    private Vector2 _targetPosition;

    // Dirección actual en la que viaja el jugador
    private Vector2 _currentDirection;

    // Referencia al Animator del jugador, usada para disparar las animaciones
    private Animator _animator;

    // Referencia al SpriteRenderer del jugador, usada para espejar el sprite cuando va a la izquierda
    private SpriteRenderer _spriteRenderer;

    [Header("VFX")]
    // Prefab del efecto de humo que se reproduce al morir el jugador
    [SerializeField] private GameObject deathSmokePrefab;

    private void Start()
    {
        // Guardamos la posición inicial para volver acá si perdemos una vida
        _startPosition = transform.position;

        // Al iniciar, nuestro primer objetivo es nuestra propia posición de inicio
        _targetPosition = transform.position;

        // Obtenemos el componente Animator del mismo GameObject
        _animator = GetComponent<Animator>();

        // Obtenemos el SpriteRenderer del mismo GameObject (sirve para flipear el sprite)
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
        {
            if (!gameManager.IsPlaying())
        {
            return;
        }

        // Si está en la animación de muerte, no procesamos movimiento ni input
        if (_isDead)
        {
            return;
        }
        // Actualizamos la orientación visual del sprite para que mire hacia donde se mueve
        UpdateVisualOrientation();

        // Solo verificamos el teclado y definimos una ruta SI ya llegamos a la celda objetivo
        if (Vector2.Distance(transform.position, _targetPosition) < 0.01f)
        {
            // Forzamos a que la posición encaje perfecto en el número entero
            transform.position = _targetPosition;

            // Le avisamos a la grilla que estamos parados acá (marca rastro si corresponde)
            gridManager.MarkTrail(transform.position);

            // Le pedimos a la grilla que intente cerrar el rastro si estamos en zona permanente
            gridManager.TryCloseTrail(transform.position);

            // Le preguntamos a la grilla si estamos parados en zona segura
            bool isOnSafeZone = gridManager.IsSafePosition(transform.position);

            // Capturamos el input de las teclas (WASD o Flechas)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            // Actualizamos la dirección. En zona vacía ignoramos el reverso exacto (marcha atrás)
            if (moveX != 0)
            {
                Vector2 candidateDirection = new Vector2(moveX, 0);
                if (isOnSafeZone || candidateDirection != -_currentDirection)
                {
                    _currentDirection = candidateDirection;
                }
            }
            else if (moveY != 0)
            {
                Vector2 candidateDirection = new Vector2(0, moveY);
                if (isOnSafeZone || candidateDirection != -_currentDirection)
                {
                    _currentDirection = candidateDirection;
                }
            }
            else if (isOnSafeZone)
            {
                // En zona segura y sin tecla apretada, frenamos el movimiento
                _currentDirection = Vector2.zero;
            }

            // Calculamos cuál sería la próxima celda teórica
            Vector2 possibleTarget = (Vector2)transform.position + _currentDirection;

            // Procesamos el movimiento según lo que haya en la celda destino
            if (_currentDirection != Vector2.zero && gridManager.IsValidPosition(possibleTarget))
            {
                if (gridManager.IsTrailCell(possibleTarget))
                {
                    // El jugador toca su propio rastro: pierde una vida
                    if (gameManager != null)
                    {
                        gameManager.HandlePlayerHit();
                    }
                }
                else
                {
                    // Celda libre: confirmamos el movimiento
                    _targetPosition = possibleTarget;
                }
            }
            else
            {
                // Si no hay para dónde ir (límite del mapa), frenamos
                _currentDirection = Vector2.zero;
            }
        }

        // Movemos físicamente al personaje hacia el objetivo frame a frame
        transform.position = Vector2.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
    }

    // Método público para reposicionar al jugador en su posición de inicio
    public void ResetToStart()
    {
        // Volvemos a la posición original
        transform.position = _startPosition;

        // Sincronizamos el objetivo y la dirección para que no quede movimiento residual
        _targetPosition = _startPosition;
        _currentDirection = Vector2.zero;

        // Reseteamos la orientación visual al estado inicial
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _spriteRenderer.flipX = false;
        
    }

    // Permite al GameManager saber si el jugador está en medio de la animación de muerte
    public bool IsDead()
    {
        return _isDead;
    }

    // Lo llama el GameManager cuando el jugador pierde una vida
    public void HandleDeath()
    {
        // Arrancamos la corutina que maneja la secuencia animación + respawn
        StartCoroutine(DeathSequence());
    }

    // Corutina que reproduce la animación de muerte y después respawnea al jugador
    private IEnumerator DeathSequence()
    {
        // Marcamos al jugador como muriendo para que no se mueva ni reciba más hits
        _isDead = true;

        // Disparamos la animación de muerte en la posición actual
        _animator.SetTrigger("Die");

        // Instanciamos el efecto de humo en la posición donde murió el jugador
        Instantiate(deathSmokePrefab, transform.position, Quaternion.identity);
    
        // Pausamos la ejecución de la corutina por la duración de la animación
        yield return new WaitForSeconds(deathAnimationDuration);

        // Después de esperar, reseteamos la posición del jugador al inicio
        ResetToStart();

        // Marcamos al jugador como vivo de nuevo para que pueda moverse otra vez
        _isDead = false;
    }

// Actualiza la rotación y el flip del sprite para que el pacman mire hacia donde se mueve
private void UpdateVisualOrientation()
{
    if (_currentDirection.x > 0)
    {
        // Movimiento hacia la derecha: rotación 0, sin flip
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _spriteRenderer.flipX = false;
    }
    else if (_currentDirection.x < 0)
    {
        // Movimiento hacia la izquierda: sin rotación pero con flip horizontal
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _spriteRenderer.flipX = true;
    }
    else if (_currentDirection.y > 0)
    {
        // Movimiento hacia arriba: rotación de 90 grados
        transform.rotation = Quaternion.Euler(0, 0, 90);
        _spriteRenderer.flipX = false;
    }
    else if (_currentDirection.y < 0)
    {
        // Movimiento hacia abajo: rotación de -90 grados
        transform.rotation = Quaternion.Euler(0, 0, -90);
        _spriteRenderer.flipX = false;
    }
    // Si la dirección es (0, 0), no cambiamos nada (mantiene la última orientación)
}

}