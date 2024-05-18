using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {

            //izquierda
            if (i % Constants.TilesPerRow != 0)
            {
                int izquierda = i - 1;
                matriu[i, izquierda] = 1;
                tiles[i].adjacency.Add(izquierda);
            }
            //derecha
            if (i % Constants.TilesPerRow != Constants.TilesPerRow - 1)
            {
                int derecha = i + 1;
                matriu[i, derecha] = 1;
                tiles[i].adjacency.Add(derecha);
            }
            //abajo
            if (i >= Constants.TilesPerRow)
            {
                int abajo = i - Constants.TilesPerRow;
                matriu[i, abajo] = 1;
                tiles[i].adjacency.Add(abajo);
            }
            //arriba
            if (i < Constants.NumTiles - Constants.TilesPerRow)
            {
                int arriba = i + Constants.TilesPerRow;
                matriu[i, arriba] = 1;
                tiles[i].adjacency.Add(arriba);
            }
        }

    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        // Obtener todas las casillas seleccionables
        List<Tile> selectableTiles = tiles.Where(tile => tile.selectable).ToList();

        // Encontrar la casilla más alejada de cualquier policía
        Tile farthestTile = FindFarthestTile(selectableTiles);

        // Mover al caco a la casilla más alejada
        robber.GetComponent<RobberMove>().MoveToTile(farthestTile);
        robber.GetComponent<RobberMove>().currentTile = farthestTile.numTile;
    }

    // Método para encontrar la casilla más alejada de cualquier policía
    private Tile FindFarthestTile(List<Tile> selectableTiles)
    {
        int maxDistance = -1;
        Tile farthestTile = null;

        foreach (Tile tile in selectableTiles)
        {
            int minDistanceToCop = int.MaxValue;

            // Encuentra la distancia mínima desde esta casilla a cualquier policía
            foreach (GameObject cop in cops)
            {
                int copTile = cop.GetComponent<CopMove>().currentTile;
                int distance = GetDistance(tile.numTile, copTile);

                // Excluir la casilla actual ocupada por un policía o el ladrón
                if (distance < minDistanceToCop && copTile != tile.numTile)
                {
                    minDistanceToCop = distance;
                }
            }

            // Si esta casilla tiene una distancia mínima mayor a la distancia máxima actual, actualizamos
            if (minDistanceToCop > maxDistance && tile.numTile != robber.GetComponent<RobberMove>().currentTile)
            {
                maxDistance = minDistanceToCop;
                farthestTile = tile;
            }
        }

        return farthestTile;
    }

    // Método para obtener la distancia entre dos casillas usando BFS
    private int GetDistance(int startTile, int endTile)
    {
        if (startTile == endTile) return 0;

        bool[] visited = new bool[Constants.NumTiles];
        Queue<int> queue = new Queue<int>();
        int[] distances = new int[Constants.NumTiles];

        queue.Enqueue(startTile);
        visited[startTile] = true;

        while (queue.Count > 0)
        {
            int currentTile = queue.Dequeue();

            foreach (int neighbor in tiles[currentTile].adjacency)
            {
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    distances[neighbor] = distances[currentTile] + 1;
                    queue.Enqueue(neighbor);

                    if (neighbor == endTile)
                    {
                        return distances[neighbor];
                    }
                }
            }
        }

        return int.MaxValue; // No se encontró un camino
    }


    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new();
        nodes.Enqueue(tiles[indexcurrentTile]);

        // Nivel actual del BFS
        int currentLevel = 0;

        // Mientras haya nodos en la cola
        while (nodes.Count > 0)
        {
            // Obtener el número de nodos en el nivel actual
            int levelNodeCount = nodes.Count;

            // Explorar los nodos del nivel actual
            for (int i = 0; i < levelNodeCount; i++)
            {
                // Obtener el siguiente nodo de la cola
                Tile currentNode = nodes.Dequeue();

                // Para cada casilla adyacente
                foreach (int adjacentTileIndex in currentNode.adjacency)
                {
                    Tile adjacentTile = tiles[adjacentTileIndex];

                    // Si la casilla adyacente no es la casilla actual, no ha sido visitada, no contiene un policía y no es la current tile de ningún otro policía
                    if (adjacentTile != currentNode && !adjacentTile.visited && !cops.Any(c => c.GetComponent<CopMove>().currentTile == adjacentTileIndex))
                    {
                        // Marcar la casilla adyacente como visitada y seleccionable
                        adjacentTile.visited = true;
                        adjacentTile.selectable = true;
                        Debug.Log(adjacentTile.numTile);

                        // Añadir la casilla adyacente a la cola para explorar sus adyacentes en el siguiente nivel
                        nodes.Enqueue(adjacentTile);
                    }
                }
            }

            // Incrementar el nivel actual
            currentLevel++;

            // Si hemos explorado los dos niveles de distancia, salir del bucle
            if (currentLevel > 1)
                break;
        }
    }
    
   
    

    

   

       
}
