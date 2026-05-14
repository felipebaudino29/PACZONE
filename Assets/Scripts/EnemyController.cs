using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Velocidad de movimiento del enemigo
    public float moveSpeed = 3f;

    // Referencia al GridManager para consultarle por las paredes
    public GridManager gridManager;

    // Dirección inicial del enemigo (normalmente diagonal)
    public Vector2 initialDirection = new Vector2(1f, 1f);

    // Distancia del centro al borde del enemigo. 0.5 = el enemigo ocupa una celda completa.
    // Si tu enemigo se ve más grande o más chico que una celda, ajustá este valor.
    public float collisionRadius = 0.5f;

    // Dirección actual en la que se mueve el enemigo
    private Vector2 _direction;

    private void Start()
    {
        // Normalizamos la dirección para que la velocidad sea consistente sin importar el ángulo
        _direction = initialDirection.normalized;
    }

    private void Update()
    {
        // Calculamos cuánto deberíamos avanzar este frame con la dirección actual
        Vector2 movement = _direction * moveSpeed * Time.deltaTime;
        Vector2 currentPosition = transform.position;

        // Chequeamos el "borde de avance" en X (el lado del enemigo en la dirección X de movimiento)
        // Si ese borde tocaría una pared al moverse, rebotamos en X
        float xLeadEdge = currentPosition.x + movement.x + Mathf.Sign(_direction.x) * collisionRadius;
        if (gridManager.IsWallForEnemy(new Vector2(xLeadEdge, currentPosition.y)))
        {
            // Invertimos la componente X de la dirección (rebote horizontal)
            _direction.x = -_direction.x;
        }

        // Hacemos el mismo chequeo para el eje Y
        float yLeadEdge = currentPosition.y + movement.y + Mathf.Sign(_direction.y) * collisionRadius;
        if (gridManager.IsWallForEnemy(new Vector2(currentPosition.x, yLeadEdge)))
        {
            // Invertimos la componente Y de la dirección (rebote vertical)
            _direction.y = -_direction.y;
        }

        // Movemos al enemigo con la dirección ya ajustada por los rebotes
        transform.position = currentPosition + _direction * moveSpeed * Time.deltaTime;
    }
}