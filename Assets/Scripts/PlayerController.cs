using UnityEngine;

public class PlayerController : MonoBehaviour
{
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

    private void Start()
    {
        // Guardamos la posición inicial para volver acá si perdemos una vida
        _startPosition = transform.position;

        // Al iniciar, nuestro primer objetivo es nuestra propia posición de inicio
        _targetPosition = transform.position;
    }

    private void Update()
    {
        // Si el juego no está en curso (victoria o derrota), no hacemos nada
        if (gameManager != null && !gameManager.IsPlaying())
        {
            return;
        }

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
    }
}