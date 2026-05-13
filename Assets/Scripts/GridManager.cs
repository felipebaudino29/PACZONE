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

    // El prefab del cuadrado visual del borde
    public GameObject cellPrefab;

    // El prefab del cuadrado visual del rastro (color/sprite diferente)
    public GameObject trailPrefab;

    // Matriz bidimensional que guarda el estado lógico de cada celda
    private CellState[,] _gridMatrix;

    // Bandera interna que indica si hay rastro sin cerrar
    private bool _hasPendingTrail = false;

    private void Start()
    {
        // Inicializamos la matriz matemática con las dimensiones asignadas
        _gridMatrix = new CellState[columns, rows];
        
        // Llamamos al método para configurar la grilla al inicio del nivel
        GenerateInitialGrid();
    }

    private void GenerateInitialGrid()
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

        // Creamos visualmente el bloque de rastro en la posición de la celda
        Instantiate(trailPrefab, position, Quaternion.identity, transform);
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

        // Convertimos todo el rastro pendiente en área capturada permanente
        ConvertAllTrailToCaptured();

        // Buscamos las áreas vacías que quedaron encerradas y rellenamos la más chica
        FillEnclosedAreas();

        // Limpiamos la bandera: ya no hay rastro pendiente
        _hasPendingTrail = false;
    }

    // Convierte todas las celdas en estado Trail a estado Captured (visualmente quedan igual)
    private void ConvertAllTrailToCaptured()
    {
        // Recorremos toda la matriz buscando celdas de rastro
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (_gridMatrix[x, y] == CellState.Trail)
                {
                    // Cambiamos solo el estado lógico (el GameObject visual no se toca)
                    _gridMatrix[x, y] = CellState.Captured;
                }
            }
        }
    }

    // Detecta los grupos de celdas vacías separados entre sí y rellena el más chico
    private void FillEnclosedAreas()
    {
        // Buscamos todos los grupos conectados de celdas vacías
        List<List<Vector2Int>> emptyComponents = FindAllEmptyComponents();

        // Si quedó un solo grupo (o ninguno) significa que no se encerró nada nuevo
        if (emptyComponents.Count < 2)
        {
            return;
        }

        // Buscamos el grupo con menos celdas: ese es el "interior" del rastro cerrado
        List<Vector2Int> smallestComponent = GetSmallestComponent(emptyComponents);

        // Rellenamos cada celda del grupo más chico
        FillCells(smallestComponent);
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

    // Convierte una lista de celdas vacías en área capturada (con visual)
    private void FillCells(List<Vector2Int> cells)
    {
        // Para cada celda del grupo seleccionado
        foreach (Vector2Int cell in cells)
        {
            // Cambiamos su estado lógico a capturada
            _gridMatrix[cell.x, cell.y] = CellState.Captured;

            // Creamos visualmente el bloque en su posición
            Vector2 position = new Vector2(cell.x, cell.y);
            Instantiate(trailPrefab, position, Quaternion.identity, transform);
        }
    }
}