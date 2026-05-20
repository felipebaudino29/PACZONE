using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Velocidad de movimiento del enemigo
    public float moveSpeed = 3f;

    // Referencia al GridManager para consultarle por las paredes
    public GridManager gridManager;

    // Referencia al GameManager para avisarle de colisiones
    public GameManager gameManager;

    // Referencia al Transform del jugador para medir distancia
    public Transform playerTransform;

    // Dirección inicial del enemigo (normalmente diagonal)
    public Vector2 initialDirection = new Vector2(1f, 1f);

    // Distancia del centro al borde del enemigo (para el rebote)
    public float collisionRadius = 0.5f;

    // Distancia a la que se considera que el enemigo golpeó al jugador
    public float playerHitDistance = 0.7f;

    // Dirección actual en la que se mueve el enemigo
    private Vector2 _direction;

    private void Start()
    {
        // Normalizamos la dirección para que la velocidad sea consistente sin importar el ángulo
        _direction = initialDirection.normalized;
    }

    private void Update()
    {
        // Si el juego no está en curso, el enemigo no se mueve ni colisiona
        if (gameManager != null && !gameManager.IsPlaying())
        {
            return;
        }

        // Antes de moverse, chequeamos si hay colisiones con el jugador o el rastro
        if (CheckCollisions())
        {
            // Si hubo colisión, ya se procesó (perdió vida + reset). Cortamos este frame.
            return;
        }

        // Calculamos cuánto deberíamos avanzar este frame con la dirección actual
        Vector2 movement = _direction * moveSpeed * Time.deltaTime;
        Vector2 currentPosition = transform.position;

        // Chequeamos rebote en X
        float xLeadEdge = currentPosition.x + movement.x + Mathf.Sign(_direction.x) * collisionRadius;
        if (gridManager.IsWallForEnemy(new Vector2(xLeadEdge, currentPosition.y)))
        {
            _direction.x = -_direction.x;
        }

        // Chequeamos rebote en Y
        float yLeadEdge = currentPosition.y + movement.y + Mathf.Sign(_direction.y) * collisionRadius;
        if (gridManager.IsWallForEnemy(new Vector2(currentPosition.x, yLeadEdge)))
        {
            _direction.y = -_direction.y;
        }

        // Movemos al enemigo con la dirección ya ajustada por los rebotes
        transform.position = currentPosition + _direction * moveSpeed * Time.deltaTime;
    }

    // Verifica si hay colisión con el jugador o con el rastro. Retorna true si hubo colisión.
    private bool CheckCollisions()
    {
        // Si no tenemos GameManager configurado, no se puede notificar la colisión, así que no chequeamos
        if (gameManager == null)
        {
            return false;
        }

        // Colisión con el jugador: medimos distancia entre los centros de ambos
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance < playerHitDistance)
            {
                gameManager.HandlePlayerHit();
                return true;
            }
        }

        // Colisión con el rastro: chequeamos si nuestra celda actual es de rastro
        if (gridManager.IsTrailCell(transform.position))
        {
            gameManager.HandlePlayerHit();
            return true;
        }

        return false;
    }
}