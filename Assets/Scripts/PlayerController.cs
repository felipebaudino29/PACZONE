using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Velocidad a la que el personaje se mueve entre celdas
    public float moveSpeed = 5f;

    // Referencia pública a nuestro GridManager para poder hacerle consultas
    public GridManager gridManager;

    // La posición exacta de la celda a la que nos estamos moviendo
    private Vector2 _targetPosition;

    // Dirección actual en la que viaja el jugador
    private Vector2 _currentDirection;

    private void Start()
    {
        // Al iniciar, nuestro primer objetivo es nuestra propia posición de inicio
        _targetPosition = transform.position;
    }

    private void Update()
    {
        // Solo verificamos el teclado y definimos una ruta SI ya llegamos a la celda objetivo
        if (Vector2.Distance(transform.position, _targetPosition) < 0.01f)
        {
            // Forzamos a que la posición encaje perfecto en el número entero
            transform.position = _targetPosition;

            // Le avisamos a la grilla que estamos parados acá (marca rastro si corresponde)
            gridManager.MarkTrail(transform.position);

            // Le pedimos a la grilla que intente cerrar el rastro si estamos en zona permanente
            gridManager.TryCloseTrail(transform.position);

            // Le preguntamos a la grilla si estamos parados en zona segura (borde o área capturada)
            bool isOnSafeZone = gridManager.IsSafePosition(transform.position);

            // Capturamos el input de las teclas (WASD o Flechas)
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            // Si hay tecla apretada, actualizamos la dirección (evitando movimiento diagonal)
            if (moveX != 0)
            {
                _currentDirection = new Vector2(moveX, 0);
            }
            else if (moveY != 0)
            {
                _currentDirection = new Vector2(0, moveY);
            }
            else if (isOnSafeZone)
            {
                // En zona segura y sin tecla apretada, frenamos el movimiento
                _currentDirection = Vector2.zero;
            }
            // Si NO estamos en zona segura y no hay input, mantenemos la dirección anterior (continuo)

            // Calculamos cuál sería la próxima celda teórica a la que queremos ir
            Vector2 possibleTarget = (Vector2)transform.position + _currentDirection;

            // Confirmamos el movimiento solo si hay dirección Y la celda destino es válida
            if (_currentDirection != Vector2.zero && gridManager.IsValidPosition(possibleTarget))
            {
                _targetPosition = possibleTarget;
            }
            else
            {
                // Si no hay para dónde ir, frenamos
                _currentDirection = Vector2.zero;
            }
        }

        // Movemos físicamente al personaje hacia el objetivo frame a frame
        transform.position = Vector2.MoveTowards(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
    }
}