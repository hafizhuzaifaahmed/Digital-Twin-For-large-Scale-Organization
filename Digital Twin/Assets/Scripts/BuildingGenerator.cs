using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
using System.Reflection.Emit;
public class GridGenerator : MonoBehaviour
{
    [SerializeField] private GameObject floorPrefab;      // Floor prefab
    [SerializeField] private GameObject wallPrefab;       // Wall prefab
    [SerializeField] private GameObject grassPlanePrefab; // Grass plane prefab
    [SerializeField] private GameObject _2ChairTable;
    [SerializeField] private GameObject _3ChairTable;
    [SerializeField] private GameObject _4ChairTable;
    [SerializeField] private GameObject _5ChairTable;
    [SerializeField] private GameObject _6ChairTable;
    [SerializeField] private GameObject Stairs;
    [SerializeField] private GameObject elevator;
    [SerializeField] private GameObject floorStairs;
    int processesNumber = 1;
    public string [] processName;
    public List<(string jobCode,string taskCode, string taskName,int order)> jobTaskList;
    public Dictionary<int, List<(int deskNo, string jobID, string job,int floorNo)>> deskAssignments;
    int roomCount;
    private int rows;                // Number of rows in the grid
    private int columns;             // Number of columns in the grid
    private int totalFloors;         // Number of total floors in the building
    private float floorSize = 4f;        // The size of each floor  (2 units per side)
    private float floorHeight = 4f;   // Height increment for each floor
    private string[] orientations;                   // To store orientation values
    private int[] numberOfTables;                    // To store numberOfTable values
    private string floorAPI = "https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/floors&CompanyCode=MLDH";
    private string roomAPI = "https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/rooms&CompanyCode=MLDH";
    
    string apiKey = "B4n0VFhrU7J5SEegAXCOYcZ3gI3YF1YW"; // API Key


    private GameObject grassParent;     // Parent GameObject for the grass plane
    public int GetTotalFloors()
    {
       return  totalFloors;
    }
    void Start()
    {
        
        StartCoroutine(FetchData());
        // Create the parent object for the grass plane and make it a child of this GameObject
        grassParent = new GameObject("GrassPlane");
        grassParent.transform.parent = this.transform;
        
        // Generate the grass plane, floors, and walls

    }
    IEnumerator FetchData()
    {
        yield return StartCoroutine(FetchTotalFloors());
        yield return StartCoroutine(FetchRoomCount());
        yield return StartCoroutine(FetchRoomDetails());
        yield return StartCoroutine(FetchProcessDetails());
    }
   
    IEnumerator FetchTotalFloors()
    {

        // Append the CompanyCode parameter to the API URL

        using (UnityWebRequest request = UnityWebRequest.Get(floorAPI))
        {
            request.SetRequestHeader("Accept", "application/json"); // Only necessary header
            request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey); // Add the authentication header
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("Floor API Response: " + responseText);

                try
                {
                    JObject jsonResponse = JObject.Parse(responseText);
                    int floorCount = 0;

                    foreach (var floor in jsonResponse["data"])
                    {
                        string floorCode = floor["FloorCode"].ToString();
                        if (floorCode.EndsWith("Gal"))
                        {
                            floorCount++;
                        }   
                    }

                    totalFloors = floorCount;
                    Debug.Log($"Total Floors: {totalFloors}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON Parsing Error: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch floors: " + request.error);
            }
        }
    }
    
    IEnumerator FetchRoomCount()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(roomAPI))
        {
            request.SetRequestHeader("Accept", "application/json"); // Only necessary header
            request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey); // Add the authentication header
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
                Debug.Log(jsonResponse);
                roomCount = 0;

                foreach (var room in jsonResponse["data"])
                {
                    string roomCode = room["RoomCode"].ToString();
                    if (roomCode.StartsWith("GAL"))
                    {
                        roomCount++;
                    }
                }

                // Since all floors have the same room count, divide by totalFloors
                int roomsPerFloor = roomCount / totalFloors;



                // Set rows and columns
                rows = 2;
                columns = roomsPerFloor / 2;
                
                Debug.Log($"Rooms Per Floor: {roomsPerFloor}, Rows: {rows}, Columns: {columns}");
            }
            else
            {
                Debug.LogError("Failed to fetch rooms: " + request.error);
            }
        }
    }
    IEnumerator FetchRoomDetails()
    {
        orientations = new string[roomCount];
        numberOfTables = new int[roomCount];
        deskAssignments = new Dictionary<int, List<(int, string, string,int)>>();

        string prefloorAPI = "https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/floors_detail&FloorCode=Fl";
        string posfloorAPI = "Gal&CompanyCode=MLDH";

        int currentIndex = 0;

        for (int i = 0; i < totalFloors; i++)
        {
            string apiUrl = $"{prefloorAPI}{i}{posfloorAPI}";
            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
            {
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Floor {i + 1} API Response: {responseText}");

                    try
                    {
                        JObject jsonResponse = JObject.Parse(responseText);
                        var rooms = jsonResponse["data"]?["Rooms"];

                        if (rooms != null)
                        {
                            List<JToken> roomList = rooms.ToList();
                            roomList = roomList.OrderBy(room => (int)room["cellNumber"]).ToList();

                            foreach (var room in roomList)
                            {
                                string orientation = room["orientation"]?.ToString();
                                if (!string.IsNullOrEmpty(orientation) && currentIndex < roomCount)
                                {
                                    orientations[currentIndex] = orientation;
                                }

                                var groupsTable = room["groups_table"];
                                List<string> jobNames = new List<string>();
                                int numberOfTable = 0;

                                if (groupsTable != null)
                                {
                                    foreach (var group in groupsTable)
                                    {
                                        if (currentIndex < roomCount)
                                        {
                                            numberOfTable = int.Parse(group["numberOfTable"].ToString());
                                            numberOfTables[currentIndex] = numberOfTable;

                                            
                                        }
                                    }
                                }


                                // Assign jobs to desks based on tableNumber
                                List<(int, string, string,int)> deskList = new List<(int, string, string,int)>();
                                var jobs = groupsTable?.FirstOrDefault()?["jobs"];

                                if (jobs != null)
                                {
                                    foreach (var job in jobs)
                                    {
                                        // Check if both 'tableNumber' and 'JobName' are not null
                                        if (job["tableNumber"] != null && job["JobCode"] != null && job["JobName"] != null)
                                        {
                                            bool isValidDeskNo = int.TryParse(job["tableNumber"].ToString(), out int deskNo);
                                            string jobCode = job["JobCode"].ToString();
                                            string jobName = job["JobName"].ToString();


                                            if (isValidDeskNo && !string.IsNullOrWhiteSpace(jobName))
                                            {

                                                deskList.Add((deskNo,jobCode, jobName,i));
                                            }
                                        }
                                    }
                                }

                                // Only add to deskAssignments if deskList has valid data
                                if (deskList.Count > 0)
                                {
                                    deskAssignments[currentIndex] = deskList;
                                }

                                currentIndex++;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"JSON Parsing Error on Floor {i + 1}: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to fetch floor {i + 1} data: {request.error}");
                }
            }
        }

        // Debug Output
        foreach (var entry in deskAssignments)
        {
            Debug.Log($"Index: {entry.Key}");
            foreach (var desk in entry.Value)
            {
                Debug.Log($"  Desk {desk.deskNo} {desk.jobID}{desk.job} {desk.floorNo}");
            }
        }
    


        GenerateGrassPlane();
        GenerateGridWithWalls();
    }

    IEnumerator FetchProcessDetails()
    {
        
        processName = new string[processesNumber]; 
        jobTaskList = new List<(string,string, string,int)>();

        string process1API = "https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/workflows&CompanyCode=MLDH&LanguageID=1&ProcessCode=HC-PR&ProcessVersion=0";
        using (UnityWebRequest request = UnityWebRequest.Get(process1API))
        {
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
                    Debug.Log(jsonResponse.ToString()); // Debug raw JSON

                    // Access the "data" array
                    var dataArray = jsonResponse["data"] as JArray;
                    if (dataArray != null)
                    {

                        foreach (var dataItem in dataArray)
                        {
                            processName[0] = dataItem["process"]?["name"]?.ToString() ?? "Unknown Process";

                            var tasks = dataItem["tasks"] as JArray; // Get the "tasks" array
                            if (tasks != null)
                            {

                                foreach (var task in tasks)
                                {
                                    int order= int.Parse (task["sort_order"]?.ToString());
                                    string taskCode = task["code"]?.ToString() ?? "Unknown";
                                    string jobCode = task["DeskCode"]?.ToString() ?? "Unknown";
                                    string taskName = task["name"]?.ToString() ?? "Unnamed Task";

                                    taskName=RemoveAmpersand(taskName);
                                    jobTaskList.Add((jobCode,taskCode, taskName,order));
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"JSON Parsing Error: {ex.Message}");
                }

                // Display the tasks in the console
                foreach (var task in jobTaskList)
                {
                    Debug.Log($"Job Code: {task.jobCode}, Task Code: {task.taskCode}  {task.taskName}");
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch process details: {request.error}, HTTP Code: {request.responseCode}");
            }
        }
    }
    public string RemoveAmpersand(string input)
    {
        if (string.IsNullOrEmpty(input)) return input; // Return if input is null or empty

        return input.Replace("amp", ""); // Remove occurrences of "amp"
    }

    void GenerateGrassPlane()
    {
        // Calculate the scale of the grass plane based on the number of rows and columns
        Vector3 grassScale = new Vector3(columns * floorSize, 1, rows * floorSize);

        // Calculate the position of the grass plane, ensuring its y position is 0
        Vector3 grassPosition = new Vector3((columns * floorSize) / 2 - floorSize / 2, -0.1f, (rows * floorSize) / 2 - floorSize / 2);

        // Instantiate the grass plane and apply the scale and position, then make it a child of grassParent
        GameObject grassPlane = Instantiate(grassPlanePrefab, grassPosition, Quaternion.identity, grassParent.transform);
        grassPlane.transform.localScale = grassScale;
        grassPlane.name = "GrassPlane";
    }

   

    void GenerateGridWithWalls()
    {

        int currentIndex = 0;
        // Get floor dimensions
        Renderer floorRenderer = floorPrefab.GetComponentInChildren<Renderer>();
        if (floorRenderer == null)
        {
            Debug.LogError("No Renderer found on floorPrefab or its children.");
            return;
        }
        Vector3 floorSize = floorRenderer.bounds.size;

        // Get wall dimensions
        Renderer wallRenderer = wallPrefab.GetComponentInChildren<Renderer>();
        if (wallRenderer == null)
        {
            Debug.LogError("No Renderer found on wallPrefab or its children.");
            return;
        }
        Vector3 wallSize = wallRenderer.bounds.size;


        
        for (int f = 0; f < totalFloors; f++)
        {
           
                float currentFloorHeight = f * floorHeight;

                GameObject floorParent = new GameObject($"Floor_{f}");
                floorParent.transform.parent = this.transform;

                GameObject floorTilesParent = new GameObject("FloorTiles");
                floorTilesParent.transform.parent = floorParent.transform;

                GameObject wallsParent = new GameObject("Walls");
                wallsParent.transform.parent = floorParent.transform;

                GameObject tablesParent = new GameObject("Tables");
                tablesParent.transform.parent = floorParent.transform;

                Debug.Log($"Floor {f}: Y = {currentFloorHeight}");

            for (int r = 0; r < rows; r++)
            {
                int mappedRow = (r == 0) ? 1 : (r == 1) ? 0 : r; // Swap row 0 and row 1

                for (int c = 0; c < columns; c++)
                {
                    if (currentIndex >= roomCount)
                    {
                        Debug.LogWarning("currentIndex exceeds roomCount. Stopping table placement.");
                        return; // Stop placing tables altogether
                    }

                    bool isElevatorPosition = (c == columns - 1 && mappedRow == rows - 2);
                    bool isStairsPosition = (c == columns - 1 && mappedRow == rows - 1);

                    if (isStairsPosition && f > 0)
                    {
                        currentIndex++;
                        continue; // Skip placing anything on stairs for floors > 0
                    }



                    Vector3 floorPosition = new Vector3(c * floorSize.x, currentFloorHeight, mappedRow * floorSize.z);
                    GameObject floor = Instantiate(floorPrefab, floorTilesParent.transform);
                    floor.transform.position = floorPosition;
                    floor.name = $"FloorTile_{currentIndex}";

                    Debug.Log($"CurrentIndex: {currentIndex}, RoomCount: {roomCount}, Orientations.Length: {orientations.Length}, NumberOfTables.Length: {numberOfTables.Length}");

                    if (currentIndex >= orientations.Length || currentIndex >= numberOfTables.Length)
                    {
                        Debug.LogError($"Index out of bounds: currentIndex = {currentIndex}, Orientations.Length = {orientations.Length}, NumberOfTables.Length = {numberOfTables.Length}");
                        return; // Stop further execution
                    }

                    GameObject tablePrefab = null;
                    int tableCount = numberOfTables[currentIndex];
                    string tableOrientation = orientations[currentIndex];

                    switch (tableCount)
                    {
                        case 2:
                            tablePrefab = _2ChairTable;
                            break;
                        case 3:
                            tablePrefab = _3ChairTable;
                            break;
                        case 4:
                            tablePrefab = _4ChairTable;
                            break;
                        case 5:
                            tablePrefab = _5ChairTable;
                            break;
                        case 6:
                            tablePrefab = _6ChairTable;
                            break;
                        default:
                            Debug.LogWarning($"Invalid table count: {tableCount}. Skipping this table.");
                            currentIndex++;
                            continue;
                    }

                    if (!isElevatorPosition)
                    {
                        if (tablePrefab != null)
                        {
                            Renderer tableRenderer = tablePrefab.GetComponentInChildren<Renderer>();
                            float tableBaseOffset = (tableRenderer != null) ? tableRenderer.bounds.min.y : 0;
                            Vector3 tablePosition = floorPosition - new Vector3(0, tableBaseOffset, 0);

                            GameObject table = Instantiate(tablePrefab, tablesParent.transform);
                            table.transform.position = tablePosition;

                            if (tableOrientation == "H")
                            {
                                table.transform.rotation = Quaternion.Euler(0, 90, 0);
                            }

                            table.name = $"Table_{currentIndex}";
                            Debug.Log($"Placed {table.name} at {table.transform.position} with orientation {tableOrientation}");

                       
                        }
                    }
                    
                    currentIndex++;
                }
            }




            if (Stairs != null)
            {
                if (f % 2 == 0)
                {
                    Renderer floorStairsRender = floorStairs.GetComponentInChildren<Renderer>();
                    float stairBaseOffset = (floorStairsRender != null) ? floorStairsRender.bounds.min.y : 0;

                    // Set stair position  
                    Vector3 stairPosition = new Vector3((columns - 1) * floorSize.x, currentFloorHeight+7.9f, (rows - 1) * floorSize.z);
                    stairPosition.y -= stairBaseOffset; // Align base with floor

                    GameObject floorStair = Instantiate(floorStairs, floorParent.transform);
                    floorStair.transform.position = stairPosition;
                    floorStair.name = $"Stair_{f}";
                }
                else
                {

                    // Get stairs bounds
                    Renderer stairRenderer = Stairs.GetComponentInChildren<Renderer>();
                    float stairBaseOffset = (stairRenderer != null) ? stairRenderer.bounds.min.y : 0;

                    // Set stair position
                    Vector3 stairPosition = new Vector3((columns - 1) * floorSize.x-2.3f, currentFloorHeight, (rows - 1) * floorSize.z-4.5f);
                    
                    stairPosition.y -= stairBaseOffset; // Align base with floor

                    GameObject stair = Instantiate(Stairs, floorParent.transform);

                    stair.transform.position = stairPosition;

                    stair.name = $"Stair_{f}";

                    Debug.Log($"Placed {stair.name} at {stair.transform.position}");
                }
            }
            else
            {
                Debug.LogError("Stairs prefab is not assigned. Please assign it in the Inspector.");
            }


            if (elevator != null)
            {
                // Get elevator bounds
                Renderer elevatorRenderer = elevator.GetComponentInChildren<Renderer>();
                float elevatorBaseOffset = (elevatorRenderer != null) ? elevatorRenderer.bounds.min.y : 0;

                // Set elevator position
                Vector3 elevatorPosition = new Vector3((columns - 1) * floorSize.x, currentFloorHeight, (rows - 2) * floorSize.z);
                elevatorPosition.y -= elevatorBaseOffset; // Align base with floor

                GameObject elevatorInstance = Instantiate(elevator, floorParent.transform);
                elevatorInstance.transform.position = elevatorPosition;
                elevatorInstance.name = $"Elevator_{f}";

                Debug.Log($"Placed {elevatorInstance.name} at {elevatorInstance.transform.position}");
            }
            else
            {
                Debug.LogError("Elevator prefab is not assigned. Please assign it in the Inspector.");
            }




            // Place walls aligned to the floor level
            float wallBaseHeight = currentFloorHeight + (wallSize.y / 2);
            PlaceWalls(rows, columns, wallBaseHeight, floorSize, wallSize, wallsParent);

            
        }
    }


    void PlaceWalls(int rows, int columns, float wallY, Vector3 floorSize, Vector3 wallSize, GameObject wallsParent)
    {
        float wallHeight = wallY;

        // ✅ Bottom and Top Walls (along the Z-axis)
        for (int c = 0; c < columns; c++)
        {
            Vector3 bottomWallPosition = new Vector3(c * floorSize.x, wallHeight, -floorSize.z / 2);
            GameObject bottomWall = Instantiate(wallPrefab, wallsParent.transform);
            bottomWall.transform.position = bottomWallPosition;
            bottomWall.transform.rotation = Quaternion.Euler(0, 90, 0);
            bottomWall.name = $"Wall_Bottom_{(int)wallY}_{c}";

            Vector3 topWallPosition = new Vector3(c * floorSize.x, wallHeight, (rows * floorSize.z) - (floorSize.z / 2));
            GameObject topWall = Instantiate(wallPrefab, wallsParent.transform);
            topWall.transform.position = topWallPosition;
            topWall.transform.rotation = Quaternion.Euler(0, 90, 0);
            topWall.name = $"Wall_Top_{(int)wallY}_{c}";
        }

        // ✅ Left and Right Walls (along the X-axis)
        for (int r = 0; r < rows; r++)
        {
            Vector3 leftWallPosition = new Vector3(-floorSize.x / 2, wallHeight, r * floorSize.z);
            GameObject leftWall = Instantiate(wallPrefab, wallsParent.transform);
            leftWall.transform.position = leftWallPosition;
            leftWall.transform.rotation = Quaternion.identity;
            leftWall.name = $"Wall_Left_{(int)wallY}_{r}";

            Vector3 rightWallPosition = new Vector3((columns * floorSize.x) - (floorSize.x / 2), wallHeight, r * floorSize.z);
            GameObject rightWall = Instantiate(wallPrefab, wallsParent.transform);
            rightWall.transform.position = rightWallPosition;
            rightWall.transform.rotation = Quaternion.identity;
            rightWall.name = $"Wall_Right_{(int)wallY}_{r}";
        }
    }
    

}
