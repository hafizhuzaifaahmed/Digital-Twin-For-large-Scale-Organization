using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject floorPrefab;      // Floor prefab
    public GameObject wallPrefab;       // Wall prefab
    public GameObject grassPlanePrefab; // Grass plane prefab
    public int rows = 2;                // Number of rows in the grid
    public int columns = 2;             // Number of columns in the grid
    public int totalFloors = 2;         // Number of total floors in the building
    public float floorSize = 4f;        // The size of each floor prefab (4 units per side)
    public float floorHeight = 4f;      // The height between floors (y-axis increment)

    private GameObject grassParent;     // Parent GameObject for the grass plane

    void Start()
    {
        // Create the parent object for the grass plane and make it a child of this GameObject
        grassParent = new GameObject("GrassPlane");
        grassParent.transform.parent = this.transform;

        // Generate the grass plane, floors, and walls
        GenerateGrassPlane();
        GenerateGridWithWalls();
    }

    void GenerateGrassPlane()
    {
        // Calculate the scale of the grass plane based on the number of rows and columns
        Vector3 grassScale = new Vector3(columns * floorSize, 1, rows * floorSize);

        // Calculate the position of the grass plane, ensuring its y position is 0
        Vector3 grassPosition = new Vector3((columns * floorSize) / 2 - floorSize / 2, 0, (rows * floorSize) / 2 - floorSize / 2);

        // Instantiate the grass plane and apply the scale and position, then make it a child of grassParent
        GameObject grassPlane = Instantiate(grassPlanePrefab, grassPosition, Quaternion.identity, grassParent.transform);
        grassPlane.transform.localScale = grassScale;
        grassPlane.name = "GrassPlane";
    }

    void GenerateGridWithWalls()
    {
        // Loop through each floor
        for (int f = 0; f < totalFloors; f++)
        {
            // Create a parent GameObject for each floor and make it a child of the GridGenerator
            GameObject floorParent = new GameObject($"Floor_{f}");
            floorParent.transform.parent = this.transform;

            // Create parent objects for FloorTiles and Walls under each floor
            GameObject floorTilesParent = new GameObject("FloorTiles");
            floorTilesParent.transform.parent = floorParent.transform;

            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.parent = floorParent.transform;

            // Calculate the y-position of the current floor
            float currentFloorHeight = f * floorHeight;

            // Loop through rows and columns to place floor tiles
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    // Calculate the position of each floor tile based on row, column, and floor height
                    Vector3 floorPosition = new Vector3(c * floorSize, currentFloorHeight, r * floorSize);

                    // Instantiate the floor prefab and make it a child of the FloorTiles parent
                    GameObject floor = Instantiate(floorPrefab, floorPosition, Quaternion.identity, floorTilesParent.transform);

                    // Assign a name to the floor GameObject based on its row, column, and floor level
                    floor.name = $"FloorTile_{f}_{r}_{c}";

                    // Place walls on the edges of the grid for the current floor, and make them children of the Walls parent
                    PlaceWalls(r, c, currentFloorHeight, wallsParent);
                }
            }
        }
    }

    void PlaceWalls(int row, int column, float currentFloorHeight, GameObject wallsParent)
    {
        Vector3 floorPosition = new Vector3(column * floorSize, currentFloorHeight, row * floorSize);

        // Left wall (-x)
        if (column == 0)
        {
            Vector3 leftWallPosition = floorPosition + new Vector3(-floorSize / 2, 0, -1);
            for (int i = 0; i < 2; i++) // Add 2 walls per side
            {
                GameObject leftWall = Instantiate(wallPrefab, leftWallPosition + new Vector3(0, 0, i * (floorSize / 2)), Quaternion.identity, wallsParent.transform);
                leftWall.name = $"Wall_Left_{row}_{column}_{i}";
            }
        }

        // Right wall (+x)
        if (column == columns - 1)
        {
            Vector3 rightWallPosition = floorPosition + new Vector3(floorSize / 2, 0, -1);
            for (int i = 0; i < 2; i++) // Add 2 walls per side
            {
                GameObject rightWall = Instantiate(wallPrefab, rightWallPosition + new Vector3(0, 0, i * (floorSize / 2)), Quaternion.identity, wallsParent.transform);
                rightWall.name = $"Wall_Right_{row}_{column}_{i}";
            }
        }

        // Bottom wall (-x, -z)
        if (row == 0)
        {
            Vector3 bottomWallPosition = new Vector3(-1, currentFloorHeight, -2); // Set to the specific position
            GameObject bottomWall = Instantiate(wallPrefab, bottomWallPosition, Quaternion.Euler(0, 90, 0), wallsParent.transform);
            bottomWall.name = $"Wall_Bottom_{row}_{column}";
        }

        // Top wall (+z)
        if (row == rows - 1)
        {
            Vector3 topWallPosition = new Vector3(-1, currentFloorHeight, 6); // Set to the specific position
            GameObject topWall = Instantiate(wallPrefab, topWallPosition, Quaternion.Euler(0, 90, 0), wallsParent.transform);
            topWall.name = $"Wall_Top_{row}_{column}";
        }
    }
}
