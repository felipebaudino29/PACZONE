using System.Collections.Generic;
using UnityEngine;

// Enumeración para definir los posibles estados lógicos de una celda
public enum CellState
{
    Empty,
    Border,
    Trail,
    Captured
}

public class GridManager : MonoBehaviour
{
    // Cantidad de columnas (ancho de la grilla)
    public int columns = 50;
    
    // Cantidad de filas (alto de la grilla)
    public int rows = 30;

    // El prefab del cuadrado visual del borde y de las áreas ya capturadas
    public GameObject cellPrefab;

    // El prefab del cuadrado visual del rastro mientras se está dibujando
    public GameObject trailPrefab;

    // Matriz bidimensional que guarda el estado lógico de cada celda
    private CellState[,] _gridMatrix;

    // Bandera interna que indica si hay rastro sin cerrar
    private bool _hasPendingTrail = false;

    // Lista de GameObjects del rastro pendiente, los necesitamos para destruirlos al cerrar
    private List<GameObject> _activeTrailObjects = new List<GameObject>();

    private void Start()
    {
        // Solo inicializamos la matriz lógica. La parte visual la dispara el GameManager al darle Play
        _gridMatrix = new CellState[columns, rows];
    }

    public void GenerateInitialGrid()
    {
        // Recorremos cada columna de la matriz
        for (int x = 0; x < columns; x++)
        {
            // Recorremos cada fila dentro de la columna actual
            for (int y = 0; y < rows; y++)
            {
                // Si estamos en la primera/última columna o primera/última fila, es el borde
                if (x == 0 || x == columns - 1 || y == 0 || y == rows - 1)
                {
                    // Marcamos la celda lógicamente como una zona segura (borde)
                    _gridMatrix[x, y] = CellState.Border;

                    // Creamos visualmente el bloque en la posición exacta (x, y)
                    Vector2 cellPosition = new Vector2(x, y);
                    Instantiate(cellPrefab, cellPosition, Quaternion.identity, transform);
                }
                else
                {
                    // Si no está en los bordes, la celda lógicamente comienza vacía
                    _gridMatrix[x, y] = CellState.Empty;
                }
            }
        }
    }

    // Método público que evalúa si una coordenada está dentro de los límites de la matriz
    public bool IsValidPosition(Vector2 targetPosition)
    {
        // Convertimos la posición matemática en números enteros para leer la matriz
        int x = Mathf.RoundToInt(targetPosition.x);
        int y = Mathf.RoundToInt(targetPosition.y);

        // Verificamos que la coordenada X no sea menor a 0 ni mayor al límite de columnas
        bool isXValid = x >= 0 && x < columns;
        
        // Verificamos que la coordenada Y no sea menor a 0 ni mayor al límite de filas
        bool isYValid = y >= 0 && y < rows;

        // La posición solo es válida si está dentro de los límites de X e Y
        return isXValid && isYValid;
    }

    // Método público que indica si una posición es zona segura (borde o área capturada)
    public bool IsSafePosition(Vector2 targetPosition)
    {
        // Primero validamos que la posición esté dentro de los límites de la grilla
        if (!IsValidPosition(targetPosition))
        {
            return false;
        }

        // Convertimos la posición matemática en números enteros para leer la matriz
        int x = Mathf.RoundToInt(targetPosition.x);
        int y = Mathf.RoundToInt(targetPosition.y);

        // La celda es segura solo si es borde o un área ya capturada (el rastro NO cuenta como segura)
        CellState state = _gridMatrix[x, y];
        return state == CellState.Border || state == CellState.Captured;
    }

    // Indica si una posición es una celda de rastro (el jugador no puede volver a pisarla)
    public bool IsTrailCell(Vector2 position)
    {
        // Si está fuera de la grilla, no es rastro
        if (!IsValidPosition(position))
        {
            return false;
        }

        // Convertimos la posición a coordenadas de matriz
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        // Es rastro solo si la celda está en estado Trail
        return _gridMatrix[x, y] == CellState.Trail;
    }

    // Indica si una posición es una pared para los enemigos (borde o área capturada)
    // IMPORTANTE: el rastro NO es pared. El enemigo lo atraviesa y al hacerlo lo destruye.
    public bool IsWallForEnemy(Vector2 position)
    {
        // Si está fuera de la grilla, la consideramos pared (así rebota en el límite)
        if (!IsValidPosition(position))
        {
            return true;
        }

        // Convertimos la posición a coordenadas de matriz
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        // El enemigo rebota solo contra el borde y áreas ya capturadas (no contra el rastro)
        CellState state = _gridMatrix[x, y];
        return state == CellState.Border || state == CellState.Captured;
    }

    // Método público que marca una celda como rastro si estaba vacía
    public void MarkTrail(Vector2 position)
    {
        // Convertimos la posición matemática en números enteros para leer la matriz
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        // Si la posición no es válida, no hacemos nada
        if (!IsValidPosition(position))
        {
            return;
        }

        // Solo marcamos rastro si la celda estaba vacía
        if (_gridMatrix[x, y] != CellState.Empty)
        {
            return;
        }

        // Actualizamos la matriz lógica al estado de rastro
        _gridMatrix[x, y] = CellState.Trail;

        // Activamos la bandera porque ahora hay rastro sin cerrar
        _hasPendingTrail = true;

        // Creamos visualmente el bloque de rastro y lo guardamos en la lista
        GameObject trailObject = Instantiate(trailPrefab, position, Quaternion.identity, transform);
        _activeTrailObjects.Add(trailObject);
    }

    // Método público que intenta cerrar el rastro pendiente si el jugador está en zona permanente
    public void TryCloseTrail(Vector2 position)
    {
        // Si no hay rastro pendiente, no hay nada que cerrar
        if (!_hasPendingTrail)
        {
            return;
        }

        // Verificamos que la posición sea válida
        if (!IsValidPosition(position))
        {
            return;
        }

        // Convertimos la posición a coordenadas de matriz
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        // Solo cerramos cuando llegamos a borde o área capturada (no si estamos en rastro)
        CellState currentState = _gridMatrix[x, y];
        if (currentState != CellState.Border && currentState != CellState.Captured)
        {
            return;
        }

        // Convertimos todo el rastro pendiente en área capturada permanente (cambia visual también)
        ConvertAllTrailToCaptured();

        // Buscamos las áreas vacías que quedaron encerradas y rellenamos la más chica sin enemigos
        FillEnclosedAreas();

        // Limpiamos la bandera: ya no hay rastro pendiente
        _hasPendingTrail = false;
    }

    // Convierte todas las celdas Trail a Captured (cambiando el visual al del borde)
    private void ConvertAllTrailToCaptured()
    {
        // Actualizamos el estado lógico de cada celda Trail
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_gridMatrix[x, y] == CellState.Trail)
                {
                    _gridMatrix[x, y] = CellState.Captured;
                }
            }
        }

        // Reemplazamos cada visual del rastro por uno de borde/área capturada
        foreach (GameObject trailObject in _activeTrailObjects)
        {
            // Tomamos la posición del visual del rastro para crear el nuevo en el mismo lugar
            Vector3 position = trailObject.transform.position;

            // Creamos un visual de borde en esa posición (estética de ladrillo)
            Instantiate(cellPrefab, position, Quaternion.identity, transform);

            // Destruimos el visual viejo del rastro
            Destroy(trailObject);
        }

        // Vaciamos la lista porque ya no hay rastro pendiente
        _activeTrailObjects.Clear();
    }

    // Detecta los grupos de celdas vacías separados entre sí y rellena el más chico SIN enemigos
    private void FillEnclosedAreas()
    {
        // Buscamos todos los grupos conectados de celdas vacías
        List<List<Vector2Int>> emptyComponents = FindAllEmptyComponents();

        // Si quedó un solo grupo (o ninguno) significa que no se encerró nada nuevo
        if (emptyComponents.Count < 2)
        {
            return;
        }

        // Obtenemos las posiciones de todos los enemigos para no tapar la zona donde están
        HashSet<Vector2Int> enemyCells = GetEnemyCells();

        // Nos quedamos solo con los grupos que NO contienen ningún enemigo
        List<List<Vector2Int>> safeComponents = new List<List<Vector2Int>>();
        foreach (List<Vector2Int> component in emptyComponents)
        {
            if (!ComponentContainsEnemy(component, enemyCells))
            {
                safeComponents.Add(component);
            }
        }

        // Si todos los grupos tienen enemigos, no rellenamos nada (caso raro)
        if (safeComponents.Count == 0)
        {
            return;
        }

        // De los grupos sin enemigos, encontramos el más chico (el "interior" del rastro)
        List<Vector2Int> smallestSafeComponent = GetSmallestComponent(safeComponents);

        // Rellenamos ese grupo
        FillCells(smallestSafeComponent);
    }

    // Devuelve las coordenadas de celda donde hay enemigos actualmente
    private HashSet<Vector2Int> GetEnemyCells()
    {
        HashSet<Vector2Int> enemyCells = new HashSet<Vector2Int>();

        // Le pedimos a Unity todos los EnemyController activos en la escena
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

        // Convertimos la posición de cada enemigo a coordenadas de celda
        foreach (EnemyController enemy in enemies)
        {
            int x = Mathf.RoundToInt(enemy.transform.position.x);
            int y = Mathf.RoundToInt(enemy.transform.position.y);
            enemyCells.Add(new Vector2Int(x, y));
        }

        return enemyCells;
    }

    // Indica si un grupo de celdas contiene la posición de algún enemigo
    private bool ComponentContainsEnemy(List<Vector2Int> component, HashSet<Vector2Int> enemyCells)
    {
        foreach (Vector2Int cell in component)
        {
            if (enemyCells.Contains(cell))
            {
                return true;
            }
        }
        return false;
    }

    // Recorre la grilla y devuelve listas de celdas vacías agrupadas por conexión
    private List<List<Vector2Int>> FindAllEmptyComponents()
    {
        // Lista resultado con todos los grupos encontrados
        List<List<Vector2Int>> components = new List<List<Vector2Int>>();

        // Matriz auxiliar para marcar celdas ya procesadas y no repetir trabajo
        bool[,] visited = new bool[columns, rows];

        // Recorremos toda la grilla buscando puntos de partida
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Si encontramos una celda vacía sin visitar, arrancamos un grupo nuevo desde ahí
                if (_gridMatrix[x, y] == CellState.Empty && !visited[x, y])
                {
                    List<Vector2Int> component = FloodFillFromCell(x, y, visited);
                    components.Add(component);
                }
            }
        }

        return components;
    }

    // Inundación: desde una celda vacía explora todas sus vecinas conectadas y las junta en un grupo
    private List<Vector2Int> FloodFillFromCell(int startX, int startY, bool[,] visited)
    {
        // Lista de celdas que forman este grupo
        List<Vector2Int> component = new List<Vector2Int>();

        // Cola de celdas pendientes de explorar (las procesamos en orden de llegada)
        Queue<Vector2Int> toExplore = new Queue<Vector2Int>();
        toExplore.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        // Procesamos hasta que no queden celdas en la cola
        while (toExplore.Count > 0)
        {
            // Tomamos la siguiente celda y la sumamos al grupo
            Vector2Int current = toExplore.Dequeue();
            component.Add(current);

            // Intentamos agregar sus 4 vecinas (arriba, abajo, izquierda, derecha) a la cola
            TryEnqueueNeighbor(current.x + 1, current.y, visited, toExplore);
            TryEnqueueNeighbor(current.x - 1, current.y, visited, toExplore);
            TryEnqueueNeighbor(current.x, current.y + 1, visited, toExplore);
            TryEnqueueNeighbor(current.x, current.y - 1, visited, toExplore);
        }

        return component;
    }

    // Agrega una celda vecina a la cola si está dentro de la grilla, está vacía y no fue visitada
    private void TryEnqueueNeighbor(int x, int y, bool[,] visited, Queue<Vector2Int> toExplore)
    {
        // Si está fuera de la grilla, no la agregamos
        if (x < 0 || x >= columns || y < 0 || y >= rows)
        {
            return;
        }

        // Solo agregamos vecinas que sean vacías y todavía no procesadas
        if (_gridMatrix[x, y] == CellState.Empty && !visited[x, y])
        {
            visited[x, y] = true;
            toExplore.Enqueue(new Vector2Int(x, y));
        }
    }

    // Recorre los grupos y devuelve el que tiene menos celdas
    private List<Vector2Int> GetSmallestComponent(List<List<Vector2Int>> components)
    {
        // Empezamos asumiendo que el primero es el más chico
        List<Vector2Int> smallest = components[0];

        // Comparamos con el resto para encontrar el realmente menor
        for (int i = 1; i < components.Count; i++)
        {
            if (components[i].Count < smallest.Count)
            {
                smallest = components[i];
            }
        }

        return smallest;
    }

    // Convierte una lista de celdas vacías en área capturada (usando el visual del borde)
    private void FillCells(List<Vector2Int> cells)
    {
        // Para cada celda del grupo seleccionado
        foreach (Vector2Int cell in cells)
        {
            // Cambiamos su estado lógico a capturada
            _gridMatrix[cell.x, cell.y] = CellState.Captured;

            // Creamos visual con el prefab del borde (mismo aspecto que el borde)
            Vector2 position = new Vector2(cell.x, cell.y);
            Instantiate(cellPrefab, position, Quaternion.identity, transform);
        }
    }
        // Calcula el porcentaje de área del mapa que está capturada (entre 0 y 1)
    public float GetCapturedPercentage()
    {
        // Total de celdas que se pueden capturar (todo lo interno, sin contar el borde)
        int playableTotal = (columns - 2) * (rows - 2);

        // Contamos cuántas celdas están actualmente en estado Captured
        int capturedCount = 0;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_gridMatrix[x, y] == CellState.Captured)
                {
                    capturedCount++;
                }
            }
        }

        // Retornamos el porcentaje como número entre 0 y 1
        return (float)capturedCount / playableTotal;
    }
    // Borra todo el rastro pendiente: las celdas Trail vuelven a Empty y se destruyen sus GameObjects
    public void ResetCurrentTrail()
    {
        // Devolvemos a Empty todas las celdas que estaban en estado Trail
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_gridMatrix[x, y] == CellState.Trail)
                {
                    _gridMatrix[x, y] = CellState.Empty;
                }
            }
        }

        // Destruimos los GameObjects visuales del rastro
        foreach (GameObject trailObject in _activeTrailObjects)
        {
            Destroy(trailObject);
        }
        _activeTrailObjects.Clear();

        // Limpiamos la bandera para que el próximo rastro arranque limpio
        _hasPendingTrail = false;
    }
}