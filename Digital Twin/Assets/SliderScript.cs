using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

using Unity.AI.Navigation;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using SimpleJSON; 

public class SliderScript : MonoBehaviour
{
    public GameObject JobPanel;
    public GameObject TaskPanel;
    public GameObject MinimizeButton;
    public GameObject AgentCameraButton;
    public GameObject CurrentTaskPrefab;
    public GameObject testButton;
    public GameObject ProcessComplete;
    public GameObject ClosingButton;
    public GameObject JobDescriptionPrefab;
    public GameObject TaskDescriptionPrefab;
    public GameObject CancelAgentButton;
    public GameObject agentPrefab;
    private NavMeshAgent singleAgent;
    public GameObject processDetailsPrefab;
    public GameObject ProcessPlayButton;
    public GameObject processTaskPrefab;   
    private GameObject processDetailsPanel;
    public GameObject processJobPrefab;     
    public Transform canvasTransform;       
    public GameObject jobPlayButtonPrefab;
    public RectTransform slideMenu;
    public GameObject ProcessNamePrefab;
    public GameObject buttonPrefab;
    public Transform buttonParent;
    public Transform buildingGenerator; 
    public Camera mainCamera;
    public Vector3 originalCameraPosition;
    public Quaternion originalCameraRotation;
    private float floorHeight = 4f;  
    public float closedX = -280f;
    public float openX = 0f;
    public float slideSpeed = 0.2f;
    private bool isMoving = false; // Track if the agent is already moving
    private bool isPaused = false;

    private Queue<(int deskNo, string taskName, string job, int roomNo, int floorNo, string JobCode, string taskCode)> taskQueue =
    new Queue<(int, string, string, int, int,string,string)>();


    private bool isOpen = false;
    private Coroutine slideCoroutine;
    private GridGenerator gridGenerator;
    public Dictionary<int, List<(int deskNo, string taskName, string job,int roomNo,int floorNo,string JobCode,string taskCode)>> mappedTasks;

    string apiKey = "B4n0VFhrU7J5SEegAXCOYcZ3gI3YF1YW"; // API Key

    void Start()
    {
        gridGenerator = FindObjectOfType<GridGenerator>();
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;
        StartCoroutine(mappingTasks());
    }
    private IEnumerator mappingTasks()
    {
        yield return new WaitForSeconds(20f); // Wait for 15 seconds before execution

        var jobTaskList = gridGenerator.jobTaskList;
        var deskAssignments = gridGenerator.deskAssignments;

        // Dictionary where key = order number, value = list of tasks with original room number
        Dictionary<int, List<(int deskNo, string taskName, string job, int roomNo, int floorNo, string JobCode, string taskCode)>> sortedMappedTasks =
            new Dictionary<int, List<(int, string, string, int, int, string, string)>>();

        foreach (var assignment in deskAssignments)
        {
            int roomNo = assignment.Key; // Original index (room number)
            List<(int deskNo, string jobID, string job, int floorNo)> deskList = assignment.Value;

            foreach (var desk in deskList)
            {
                var matchingTask = jobTaskList.FirstOrDefault(task => task.jobCode == desk.jobID);

                if (!string.IsNullOrEmpty(matchingTask.taskName))
                {
                    int orderNumber = matchingTask.order;

                    if (!sortedMappedTasks.ContainsKey(orderNumber))
                    {
                        sortedMappedTasks[orderNumber] = new List<(int, string, string, int, int, string, string)>();
                    }

                    // Add the task while preserving the original room number
                    sortedMappedTasks[orderNumber].Add((desk.deskNo, matchingTask.taskName, desk.job, roomNo, desk.floorNo, matchingTask.jobCode, matchingTask.taskCode));
                }
            }
        }

        // Sort dictionary by order number
        mappedTasks = sortedMappedTasks.OrderBy(kvp => kvp.Key)
                                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


    }
    private void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
        {
            StartCoroutine(ProcessNextTask()); // Move the agent when Space is pressed
        }
        
        
        if(isPaused)
        {
            DestroyExistingAgents();
        }
    }


    public void GenerateButtons()
    {
        int totalFloors = gridGenerator.GetTotalFloors();
        GenerateFloorButtons(totalFloors);
    }

    public void ToggleMenu()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        float targetX = isOpen ? closedX : openX;
        slideCoroutine = StartCoroutine(SlideMenu(targetX));
        isOpen = !isOpen;
    }

    private IEnumerator SlideMenu(float targetX)
    {
        Vector2 startPos = slideMenu.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);
        float elapsedTime = 0f;

        while (elapsedTime < slideSpeed)
        {
            elapsedTime += Time.deltaTime;
            slideMenu.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsedTime / slideSpeed);
            yield return null;
        }

        slideMenu.anchoredPosition = targetPos;
    }

    void GenerateFloorButtons(int totalFloors)
    {
        mainCamera.transform.SetParent(null);

        GameObject TestButton = GameObject.Find("TestButton(Clone)");
        if (TestButton != null)
        {
            Destroy(TestButton.gameObject);
        }

        GameObject ProcessDetails = GameObject.Find("ProcessDetailsPrefab(Clone)");
        if (ProcessDetails != null)
        {
            ProcessDetails.SetActive(false);
        }
        // Clear existing children
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < totalFloors; i++)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent);
            TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Floor " + i;
            }

            RectTransform rectTransform = newButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                
                 rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - (55 * i));
                
            }

            int floorIndex = i;
            Button buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => SelectFloor(floorIndex));
            }
            Transform buildingGenerator = GameObject.Find("BuildingGenerator").transform;

            for (int j = 0; j < buildingGenerator.childCount; j++)
            {
                Transform floor = buildingGenerator.GetChild(j);
                if (floor.name == "GrassPlane") continue;

                floor.gameObject.SetActive(true);
            }
        }
    }

    public void ShowProcessesList()
    {
        
        Transform buildingGenerator = GameObject.Find("BuildingGenerator").transform;

        GameObject TestButton = GameObject.Find("TestButton(Clone)");
        if (TestButton != null)
        {
            Destroy(TestButton.gameObject);
        }

        for (int j = 0; j < buildingGenerator.childCount; j++)
        {
            Transform floor = buildingGenerator.GetChild(j);
            if (floor.name == "GrassPlane") continue;

            floor.gameObject.SetActive(true);
        }
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
            mainCamera.transform.SetParent(null); // Unparent the camera
            Debug.Log("Camera has been detached from the agent before destruction.");
        }
        // Clear existing children
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in canvasTransform)
        {
            if (child.name.Contains("ProcessDetailsPrefab"))
            {
                Destroy(child.gameObject);
            }
        }
        GameObject existingPanel = GameObject.Find("FloorPanel");
        if (existingPanel != null) Destroy(existingPanel);


        var processNames = gridGenerator.processName;

        for (int i = 0; i < processNames.Length; i++)
        {
            GameObject newTextObject = Instantiate(ProcessNamePrefab, buttonParent);
            TMP_Text tmpText = newTextObject.GetComponent<TMP_Text>();

            if (tmpText != null)
            {
                tmpText.text = (i+1)+". "+processNames[i];
            }

            RectTransform rectTransform = newTextObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    rectTransform.anchoredPosition.x,
                    rectTransform.anchoredPosition.y - (55 * i)
                );
            }

            // Add Button functionality
            Button button = newTextObject.GetComponent<Button>();
            if (button != null)
            {
                int index = i;  // Capture the current value of i
                button.onClick.AddListener(() => clickedOnProcess(index));
            }
        }
    }


    public void clickedOnProcess(int ind)
    {

        GenerateProcessUI();
    }

    private void GenerateProcessUI()
    {
        // Destroy existing ProcessDetailsPrefab instances
        foreach (Transform child in canvasTransform)
        {
            if (child.name.Contains("ProcessDetailsPrefab"))
            {
                Destroy(child.gameObject);
            }
        }

        float itemSpacing = 60f;
        float buttonOffsetX = -20f; // Horizontal offset to position button beside the job

        // Instantiate ProcessDetailsPrefab only once
        GameObject processDetails = Instantiate(processDetailsPrefab, canvasTransform);

        // Find PanelContent inside the prefab
        Transform panelContent = processDetails.transform.Find("PanelContent");
        if (panelContent == null)
        {
            Debug.LogError("PanelContent not found in ProcessDetailsPrefab!");
            return;
        }

        // Instantiate ProcessPlayButton inside PanelContent
        GameObject playButtonObject = Instantiate(ProcessPlayButton, panelContent);
        Button processPlayButton = playButtonObject.GetComponent<Button>();

        if (processPlayButton != null)  
        {
            processPlayButton.onClick.AddListener(OnPlayProcessClick);
        }

        // Optional: Reset position if you want it to keep the prefab's default position
        RectTransform processButtonRect = playButtonObject.GetComponent<RectTransform>();
        


        // Find TaskContent and JobContent inside the prefab
        Transform taskContent = processDetails.transform.Find("PanelContent/TaskContent");
        Transform jobContent = processDetails.transform.Find("PanelContent/JobContent");

        if (taskContent == null || jobContent == null)
        {
            Debug.LogError("TaskContent or JobContent not found in ProcessDetailsPrefab!");
            return;
        }

        Transform PanelHeading = processDetails.transform.Find("heading");
        if (PanelHeading != null)
        {
            GameObject ClosingPanelButton = Instantiate(ClosingButton, PanelHeading);
            Button closeButton = ClosingPanelButton.GetComponent<Button>();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    Destroy(processDetails.gameObject);
                    return;
                });
            }

            GameObject MinPrefab=Instantiate(MinimizeButton, PanelHeading);
            if (MinPrefab != null)
            {
                Button MinimizeBtn = MinPrefab.GetComponent<Button>();
                if(MinimizeBtn!=null)
                {
                    MinimizeBtn.onClick.AddListener(MinimizeProcessPanel);
                }
            }
        }
        float taskYPosition = -60f;
        float jobYPosition = -60f;

        // Loop through mappedTasks dictionary to populate tasks and jobs
        int i = 0;
        foreach (var entry in mappedTasks)
        {
            foreach (var task in entry.Value)
            {
                // Instantiate ProcessJTaskPrefab for Task Name
                GameObject taskItem = Instantiate(processTaskPrefab, taskContent);
                taskItem.name = $"processTaskPrefab{i}";
                TextMeshProUGUI taskText = taskItem.GetComponentInChildren<TextMeshProUGUI>();
                if (taskText != null)
                {
                    taskText.text = $"{entry.Key}. {task.taskName} (Room: {task.roomNo})"; // Use dictionary key as order
                }

                // Add spacing for task
                RectTransform taskRect = taskItem.GetComponent<RectTransform>();
                if (taskRect != null)
                {
                    taskRect.anchoredPosition = new Vector2(taskRect.anchoredPosition.x, taskYPosition);
                    taskYPosition -= itemSpacing;
                }
                Button taskButton=taskItem.AddComponent<Button>();
                if (taskButton != null)
                {
                    taskButton.onClick.AddListener(()=>showTaskDetails(task.taskCode,task.taskName));
                }
                // Instantiate ProcessJobPrefab for Job Name
                GameObject jobItem = Instantiate(processJobPrefab, jobContent);
                jobItem.name = $"processJobPrefab{i}";
                TextMeshProUGUI jobText = jobItem.GetComponentInChildren<TextMeshProUGUI>();
                if (jobText != null)
                {
                    jobText.text = task.job; // Display the job name
                }

                // Set position for job (same Y-axis for alignment)
                RectTransform jobRect = jobItem.GetComponent<RectTransform>();
                if (jobRect != null)
                {
                    jobRect.anchoredPosition = new Vector2(jobRect.anchoredPosition.x, jobYPosition);
                }

                Button jobButton = jobItem.AddComponent<Button>();
                if (jobButton != null)
                {
                    jobButton.onClick.AddListener(()=>showJobDetails(task.JobCode,task.job));
                }
                // Instantiate JobPlayButton on the same line as the job
                GameObject jobPlayButton = Instantiate(jobPlayButtonPrefab, jobContent);
                Button playButton = jobPlayButton.GetComponent<Button>();

                if (playButton != null)
                {
                    playButton.onClick.AddListener(() => OnJobPlayButtonClick(task.deskNo,task.taskName,task.job,task.roomNo,task.floorNo));
                }

                // Set position for button next to job (same Y-axis)
                RectTransform buttonRect = jobPlayButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = new Vector2(jobRect.anchoredPosition.x + buttonOffsetX, jobYPosition+25);
                }

                // Adjust Y position for the next set
                jobYPosition -= itemSpacing;
                i++;
            }
        }
    }

    // Function to handle button click event
    private void MinimizeProcessPanel()
    {
        GameObject processPanel = GameObject.Find("ProcessDetailsPrefab(Clone)");
        if (processPanel != null) {
            processPanel.SetActive(false);
        }
    }
    private void showTaskDetails(string taskCode, string taskName)
    {
        Debug.Log($"The task is clicked: {taskCode}");
        StartCoroutine(FetchTaskDetails(taskCode, taskName));
    }

    private IEnumerator FetchTaskDetails(string taskCode, string taskName)
    {
        string apiUrl = $"https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/task_content&TaskCode={taskCode}&CompanyCode=MLDH&LanguageID=1&TaskVersion=0";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("Accept", "application/json"); // Only necessary header
            request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey); // Add the authentication header
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch task details: {request.error}");
            }
            else
            {
                JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
                string details = jsonResponse["data"]?["Details"]?.ToString();

                if (!string.IsNullOrEmpty(details))
                {
                    string cleanedText = CleanApiText(details);
                    Debug.Log($"Cleaned Task Details:\n{cleanedText}");
                    DisplayTaskDetails(cleanedText, taskName);
                }
                else
                {
                    Debug.LogWarning("No details found for the task.");
                }
            }

        }
    }

    private static string CleanApiText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove special API tags (++section++, ++sectiontitle++, ++endsection++)
        text = Regex.Replace(text, @"\+\+[^+]+\+\+", "");

        // Remove <size=100%> and </size> dynamically
        text = Regex.Replace(text, @"<size=\d+%>|</size>", "");

        // Convert <br> to newline for readability
        text = text.Replace("<br>", "\n");

        // Remove empty <b><br></b> tags that create extra new lines
        text = Regex.Replace(text, @"<b>\s*<br>\s*</b>", "\n");

        // Remove remaining <b> and </b>
        text = Regex.Replace(text, @"</?b>", "");

        // Remove extra blank lines (multiple newlines -> single newline)
        text = Regex.Replace(text, @"\n\s*\n+", "\n");

        return text.Trim();
    }


    private void DisplayTaskDetails(string text, string taskName)
    {
        foreach (Transform child in canvasTransform)
        {
            if (child.name.Contains("TaskDescriptionPrefab(Clone)"))
            {
                Destroy(child.gameObject);
            }
        }

        GameObject existingPanel = GameObject.Find("ProcessDetailsPrefab(Clone)");
        if (existingPanel != null)
        {
            existingPanel.SetActive(false);
        }
        GameObject taskDescription = Instantiate(TaskDescriptionPrefab, canvasTransform);
        if (taskDescription != null)
        {
            Transform taskNameContent = taskDescription.transform.Find("PanelContent/taskName");
            Transform taskDescriptionContent = taskDescription.transform.Find("PanelContent/TaskContent");

            TextMeshProUGUI taskText = taskNameContent.GetComponentInChildren<TextMeshProUGUI>();
            if (taskText != null)
            {
                taskText.text = taskName;   
            }

            TextMeshProUGUI taskDescriptionText = taskDescriptionContent.GetComponentInChildren<TextMeshProUGUI>();
            if (taskDescriptionText != null)
            {
                taskDescriptionText.text = text;
            }

            Transform HeadingsClosingButton = taskDescription.transform.Find("heading");
            if (HeadingsClosingButton != null)
            {
                GameObject ClosingPanelButton = Instantiate(ClosingButton, HeadingsClosingButton);
                Button closeButton = ClosingPanelButton.GetComponent<Button>();

                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(() => 
                    { 
                        Destroy(taskDescription.gameObject);
                        existingPanel.SetActive(true); 
                    });
                }
            }

        }
    }

    private void showJobDetails(string jobCode,string JobName)
    {
        Debug.Log($"The job is clicked: {jobCode}");
        StartCoroutine(FetchJobDetails(jobCode, JobName));
    }

    private IEnumerator FetchJobDetails(string jobCode, string JobName)
    {
        string apiUrl = $"https://cms3d.crystal-system.eu/index.php?route=rest/3d_admin/jobs_content&DeskCode={jobCode}&CompanyCode=MLDH&LanguageID=1&SpecializationCode=000";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            request.SetRequestHeader("Accept", "application/json"); // Only necessary header
            request.SetRequestHeader("X-Oc-Restadmin-Id", apiKey); // Add the authentication header
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch job details: {request.error}");
            }
            else
            {
                JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
                string details = jsonResponse["data"]?["Details"]?.ToString();

                if (!string.IsNullOrEmpty(details))
                {
                    string cleanedText = CleanApiText(details);
                    Debug.Log($"Cleaned job Details:\n{cleanedText}");
                    DisplayJobDetails(cleanedText, JobName);
                }
                else
                {
                    Debug.LogWarning("No details found for the job.");
                }
            }
        }
    }

    private void DisplayJobDetails(string text, string JobName)
    {
        foreach (Transform child in canvasTransform)
        {
            if (child.name.Contains("JobDescriptionPrefab(Clone)"))
            {
                Destroy(child.gameObject);
            }
        }

        GameObject existingPanel = GameObject.Find("ProcessDetailsPrefab(Clone)");
        if (existingPanel != null)
        {
            existingPanel.SetActive(false);
        }
        GameObject jobDescription = Instantiate(JobDescriptionPrefab, canvasTransform);
        if (jobDescription != null)
        {
            Transform jobNameContent = jobDescription.transform.Find("PanelContent/JobName");
            Transform jobDescriptionContent = jobDescription.transform.Find("PanelContent/JobContent");

            TextMeshProUGUI jobText = jobNameContent.GetComponentInChildren<TextMeshProUGUI>();
            if (jobText != null)
            {
                jobText.text = JobName;
            }

            TextMeshProUGUI jobDescriptionText = jobDescriptionContent.GetComponentInChildren<TextMeshProUGUI>();
            if (jobDescriptionText != null)
            {
                jobDescriptionText.text = text;
            }

            Transform HeadingsClosingButton = jobDescription.transform.Find("heading");
            if (HeadingsClosingButton != null)
            {
                GameObject ClosingPanelButton = Instantiate(ClosingButton, HeadingsClosingButton);
                Button closeButton = ClosingPanelButton.GetComponent<Button>();

                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(() =>
                    {
                        Destroy(jobDescription.gameObject);
                        existingPanel.SetActive(true);
                    });
                }
            }

        }
    }


    private void OnJobPlayButtonClick(int deskNo, string taskName, string job, int roomNo,int floorNo)
    {
        Debug.Log($"Job Play Button Clicked. Entry: {deskNo}, {taskName}, {roomNo}");

        // Step 1: Find or Create a NavMeshSurface (Avoid recreating multiple surfaces)
        NavMeshSurface navMeshSurface = FindObjectOfType<NavMeshSurface>();
        if (navMeshSurface == null)
        {
            Debug.LogError("No NavMeshSurface found! Ensure the NavMesh is baked before runtime.");
            return;
        }

        // Step 2: Ensure the NavMesh is built
        navMeshSurface.BuildNavMesh();
        DestroyExistingAgents();

        // Step 3: Create a Capsule for the NavMeshAgent
        GameObject agentObject = Instantiate(agentPrefab, new Vector3(0, 1, -3), Quaternion.identity);
        agentObject.name = "NavMeshAgent_" + deskNo;
        agentObject.tag = "NavMeshAgent";

        // Step 4: Add a NavMeshAgent component and configure it
        NavMeshAgent agent = agentObject.AddComponent<NavMeshAgent>();
        agent.speed = 3.0f; // Increased speed for better movement
        agent.radius = 0.2f;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.0f; // Prevents overshooting the target

        // Step 5: Set a valid destination
       StartCoroutine(HandleAgentLifecycle(agent, deskNo, roomNo, floorNo));
       

    }
    private IEnumerator HandleAgentLifecycle(NavMeshAgent agent, int deskNo, int roomNo, int floorNo)
    {
        // Instantiate cancel button from prefab
        GameObject cancelProcess = Instantiate(CancelAgentButton, canvasTransform);

        // Get the button component that already exists in the prefab
        Button cancelButton = cancelProcess.GetComponent<Button>();

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() => CancelAgent(agent, cancelProcess));
        }

        // Move the agent to destination

        //Attach the camera to the agent button
        GameObject AgentCamera = Instantiate(AgentCameraButton, canvasTransform);
        if (AgentCamera != null)
        {
            Button AgentCameraButton = AgentCamera.GetComponent<Button>();
            if (AgentCameraButton != null)
            {
                AgentCameraButton.onClick.AddListener(() => AttachCameraToAgent(agent.transform));
            }
        }
        yield return StartCoroutine(SetAgentDestination(agent, deskNo, roomNo, floorNo));

        // Wait until the agent reaches its destination
        while (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
        CancelAgent(agent, cancelProcess);



    }
    private void CancelAgent(NavMeshAgent agent, GameObject cancelButton)
    {
        if (agent != null)
        {
            agent.isStopped = true;
            Debug.Log($"Cancelling agent: {agent.gameObject.name}");
            isPaused=true;
           
            // Destroy the agent
            DestroyExistingAgents();
          }

         
        // Destroy the cancel button UI
        Destroy(cancelButton);
        GameObject AttachCameraBtn = GameObject.Find("AttachCameraAgentButton(Clone)");
        if (AttachCameraBtn != null)
        {
            Destroy(AttachCameraBtn.gameObject);
        }
        GameObject FloorPanel = GameObject.Find("FloorPanel");
        if (FloorPanel != null)
        {
            Destroy(FloorPanel.gameObject);
        }
    }

    private void DestroyExistingAgents()
    {
        GameObject[] existingAgents = GameObject.FindGameObjectsWithTag("NavMeshAgent");

        if (existingAgents.Length == 0)
        {
            return;
        }
        processDetailsPanel.SetActive(true);

        foreach (GameObject agent in existingAgents)
        {
            // Check if the main camera is a child of the agent
            if (mainCamera != null)
            {
                mainCamera.transform.position = originalCameraPosition;
                mainCamera.transform.rotation = originalCameraRotation;
                mainCamera.transform.SetParent(null); // Unparent the camera
                Debug.Log("Camera has been detached from the agent before destruction.");
            }

            Destroy(agent);
            Debug.Log($"Destroyed existing agent: {agent.name}");
            isPaused=false;
        }
    }


    // Call this function instead of directly destroying the agent
    
    private IEnumerator SetAgentDestination(NavMeshAgent agent, int deskNo, int roomNo, int floorNo)
    {
        yield return new WaitForSeconds(0.5f); // Small delay for stability

       
        Animator animator = agent.GetComponent<Animator>();
        if (animator != null)
        {
            
          animator.enabled = true;
        }
        

        string floorName = $"Floor_{floorNo}";
        string roomName = $"Table_{roomNo}";
        string deskName = $"Desk{deskNo}";

        // Find the floor (even if it's inactive)
        GameObject floorObject = FindInactiveObjectByName(floorName);
        if (floorObject == null)
        {
            Debug.LogError($"Floor {floorName} not found in hierarchy.");
            yield break;
        }

        bool wasFloorInactive = !floorObject.activeSelf; // Check if it was inactive
        if (wasFloorInactive) floorObject.SetActive(true); // Temporarily activate


        Transform roomTransform = floorObject.transform.Find($"Tables/{roomName}");
        if (roomTransform == null)
        {
            Debug.LogError($"Room {roomName} not found under {floorName}.");
            yield break;
        }

        Transform deskTransform = roomTransform.Find(deskName);
        if (deskTransform == null)
        {
            Debug.LogError($"Desk {deskName} not found under {roomName}.");
            yield break;
        }

        Vector3 destination = deskTransform.position;
        if (wasFloorInactive) floorObject.SetActive(false);


        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            // Dynamically find the ProcessDetailsPrefab panel from Canvas
            if (processDetailsPanel == null)
            {
                Transform canvasTransform = GameObject.Find("Canvas")?.transform;
                if (canvasTransform != null)
                {
                    processDetailsPanel = canvasTransform.Find("ProcessDetailsPrefab(Clone)")?.gameObject;
                }

                if (processDetailsPanel == null)
                {
                    Debug.LogError("ProcessDetailsPrefab panel not found under Canvas.");
                    yield break;
                }
            }

            // Hide ProcessDetailsPrefab when agent starts moving
            processDetailsPanel.SetActive(false);
             
            
            
            agent.SetDestination(hit.position);
            Debug.Log($"Agent {agent.name} is moving to {hit.position}");

           
            // Attach camera to the agent
            yield return StartCoroutine(CheckAgentArrival(agent, hit.position));
            GameObject AttachCameraBtn = GameObject.Find("AttachCameraAgentButton(Clone)");
            if (AttachCameraBtn != null)
            {
                Destroy(AttachCameraBtn.gameObject);
            }
            GameObject FloorPanel = GameObject.Find("FloorPanel");
            if (FloorPanel != null)
            {
                Destroy (FloorPanel.gameObject);
            }

        }
        else
        {
            Debug.LogError($"No valid NavMesh position found near {destination}");
        }
    }
    private IEnumerator CheckAgentArrival(NavMeshAgent agent, Vector3 targetPosition,string taskName=null,string JobName=null)
    {
        while (!isPaused)
        {
            // Ensure the agent still exists and is on a valid NavMesh
            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.LogWarning("Agent no longer exists or is not on NavMesh. Stopping coroutine.");
                yield break; // Exit the coroutine safely
            }

            // Wait until the agent is not calculating a path
            if (!agent.pathPending)
            {
                // Check if the agent has reached the destination
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    // Ensure the agent has completely stopped moving
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        GameObject AgentCameraButton = FindInactiveObjectByName("AttachCameraAgentButton(Clone)");
                        if (AgentCameraButton != null)
                        {
                            Destroy(AgentCameraButton.gameObject);
                        }
                        if (taskName != null && JobName != null)
                        {
                            GameObject taskUI = Instantiate(TaskPanel, canvasTransform);
                            if (taskUI != null)
                            {
                                TextMeshProUGUI task = taskUI.GetComponentInChildren<TextMeshProUGUI>();
                                if (task != null)
                                {
                                    task.text = taskName; // Completed task color
                                }
                            }
                            GameObject JobUI = Instantiate(JobPanel, canvasTransform);
                            if (JobUI != null)
                            {
                                TextMeshProUGUI job = JobUI.GetComponentInChildren<TextMeshProUGUI>();
                                if (job != null)
                                {
                                    job.text = JobName; // Completed task color
                                }
                            }
                            
                        }
                        Animator animator = agent.GetComponent<Animator>();
                        if (animator != null)
                        { animator.enabled = false; }
                        break; 

                       


                    }
                }
            }
            yield return null;
        }

        // When agent reaches the destination, show the panel again
        if (processDetailsPanel != null)
        {
            processDetailsPanel.SetActive(true);
        }
    }


    private void AttachCameraToAgent(Transform agentTransform)
    {
        if (mainCamera == null)
        {
            Debug.LogError("MainCamera is not assigned!");
            return;
        }   
        // Optional: Make camera a child of the agent to follow it
        mainCamera.transform.SetParent(agentTransform);
        // Set camera's position in world space relative to the agent
        Vector3 cameraOffset = new Vector3(-3, 3.3f, -3); // Above agent
        mainCamera.transform.position = agentTransform.position + cameraOffset;
        mainCamera.transform.localRotation = Quaternion.Euler(25, 0, 0); // Adjust camera angle as needed

        GameObject attachCameraBtn = GameObject.Find("AttachCameraAgentButton(Clone)");
        if (attachCameraBtn != null)
        {
           attachCameraBtn.SetActive(false);
        }
        Debug.Log("Camera attached to agent.");
    }
    public void OnPlayProcessClick()
    {
        currentTask = 0;
        DestroyExistingAgents();


        // Create the agent only if it doesn't exist
        GameObject agentObject = Instantiate(agentPrefab, new Vector3(0, 1, -3), Quaternion.identity);
        agentObject.name = "NavMeshAgent_Single";
        agentObject.tag = "NavMeshAgent";
        singleAgent=agentObject.AddComponent<NavMeshAgent>();   
        singleAgent.speed = 3.0f;
        singleAgent.radius = 0.2f;
        singleAgent.angularSpeed = 120f;
        singleAgent.acceleration = 8f;
        singleAgent.stoppingDistance = 0.01f;

        // Build the navmesh if needed
        NavMeshSurface navMeshSurface = FindObjectOfType<NavMeshSurface>();
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        GameObject ProcessDetails = GameObject.Find("TaskContent");

        GameObject CurrentTask = Instantiate(CurrentTaskPrefab, ProcessDetails.transform);

        PrepareTaskQueue(); // Setup tasks but don't start movement immediately
        GameObject cancelProcess = Instantiate(CancelAgentButton, canvasTransform);

        // Get the button component that already exists in the prefab
        Button cancelButton = cancelProcess.GetComponent<Button>();

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() => CancelAgent(singleAgent, cancelProcess));
        }

        //Attach the camera to the agent button
        GameObject AgentCamera = Instantiate(AgentCameraButton, canvasTransform);
        if (AgentCamera != null)
        {
            Button AgentCameraButton = AgentCamera.GetComponent<Button>();
            if (AgentCameraButton != null)
            {
                AgentCameraButton.onClick.AddListener(() => AttachCameraToAgent(singleAgent.transform));
            }
        }
        GameObject ProcessDetailPanel = GameObject.Find("ProcessDetailsPrefab(Clone)");
        if (ProcessDetailPanel != null)
        {
            RectTransform rectTransform = ProcessDetailPanel.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                // Change scale
                rectTransform.localScale = new Vector3(0.55f, 0.55f, 1f);

                // Change position
                rectTransform.anchoredPosition = new Vector2(-585f, -270f);
            }
            else
            {
                Debug.LogError("❌ RectTransform not found on ProcessDetailPanel!");
            }
        }


    }

    private void PrepareTaskQueue()
    {
        taskQueue.Clear(); // Reset the task queue

        foreach (var entry in mappedTasks)
        {
            foreach (var task in entry.Value)
            {
                taskQueue.Enqueue(task);
            }
        }
    }
    int currentTask = 0;
    private IEnumerator ProcessNextTask()
    {
        GameObject taskUI = GameObject.Find("TaskPanel(Clone)");
        if (taskUI != null)
        {
            Destroy(taskUI.gameObject);
        }
        GameObject JobUI = GameObject.Find("JobPanel(Clone)");
        if (JobUI != null)
        {
            Destroy(JobUI.gameObject);
        }
        if (taskQueue.Count == 0)
        {

            GameObject panelContent = GameObject.Find("PanelContent");

            GameObject completeProcess = Instantiate(ProcessComplete, panelContent.transform);
            isPaused = true;
            Debug.Log("All tasks completed.");

            GameObject agentCancelB = GameObject.Find("CancelAgentButton(Clone)");
            if (agentCancelB != null)
            {
                Destroy(agentCancelB);
            }
            ////Showing the test Button
            StartAvatar avatarScript;
            avatarScript = FindObjectOfType<StartAvatar>();
            if (avatarScript == null) {
                yield break;
            }
            GameObject freeAvatar = GameObject.Find("FreeAvatar");
            GameObject testclick = Instantiate(testButton, freeAvatar.transform);

            Button testButto = testclick.GetComponent<Button>();
            if (testButto != null)
            {
                testButto.onClick.AddListener(() => avatarScript.SpawnAgent());
            }
            
            yield break;
        }
        // Ensure previous task/job turn grey
        if (currentTask >= 1) // Avoid changing colors when currentTask is 0 initially
        {
            GameObject prevTask = GameObject.Find($"processTaskPrefab{currentTask - 1}");
            GameObject prevJob = GameObject.Find($"processJobPrefab{currentTask - 1}");

            if (prevTask != null)
            {
                TextMeshProUGUI prevTaskText = prevTask.GetComponentInChildren<TextMeshProUGUI>();
                if (prevTaskText != null)
                {
                    prevTaskText.color = Color.gray; // Completed task color
                }
            }

            if (prevJob != null)
            {
                TextMeshProUGUI prevJobText = prevJob.GetComponentInChildren<TextMeshProUGUI>();
                if (prevJobText != null)
                {
                    prevJobText.color = Color.gray; // Completed job color
                }
            }
        }

        // Update the current task/job to red
        GameObject currentTaskObj = GameObject.Find($"processTaskPrefab{currentTask}");
        GameObject currentJobObj = GameObject.Find($"processJobPrefab{currentTask}");

        if (currentTaskObj != null)
        {
            TextMeshProUGUI currentTaskText = currentTaskObj.GetComponentInChildren<TextMeshProUGUI>();
            if (currentTaskText != null)
            {
                currentTaskText.color = Color.red; // Highlight active task
            }
        }

        if (currentJobObj != null)
        {
            TextMeshProUGUI currentJobText = currentJobObj.GetComponentInChildren<TextMeshProUGUI>();
            if (currentJobText != null)
            {
                currentJobText.color = Color.red; // Highlight active job
            }
        }

        // Move the "CurrentTaskInProcess(Clone)" down by 60
       if(currentTask>0)
        {
            GameObject currentTaskProcess = GameObject.Find("CurrentTaskInProcess(Clone)");
            if (currentTaskProcess != null)
            {
                RectTransform rect = currentTaskProcess.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition += new Vector2(0, -60);
                }
            }
        }

        // Increment task count
        currentTask++;



        var task = taskQueue.Dequeue(); // Get the next task
        Debug.Log($"Moving agent to Desk {task.deskNo}");

        if (singleAgent == null)
        {
            Debug.LogError("Agent is missing! Stopping process.");
            yield break;
        }

        isMoving = true;

        // Move the agent to the desk location
        yield return StartCoroutine(SetAgentDestination(singleAgent, task.deskNo, task.roomNo, task.floorNo));

        // Wait until the agent reaches the destination
        yield return StartCoroutine(CheckAgentArrival(singleAgent, singleAgent.destination,task.taskName,task.job));

        isMoving = false;
    }





    void SelectFloor(int floorIndex)
    {
        if (mainCamera != null)
        {
            Vector3 newPosition = new Vector3(17.2f, 25f + (4f * floorIndex), 5.32f);
            Quaternion newRotation = Quaternion.Euler(90f, 0f, 0f);
            StartCoroutine(SmoothMoveCamera(newPosition, newRotation));
        }

        Transform buildingGenerator = GameObject.Find("BuildingGenerator").transform;

        for (int i = 0; i < buildingGenerator.childCount; i++)
        {
            Transform floor = buildingGenerator.GetChild(i);
            if (floor.name == "GrassPlane") continue;

            int actualFloorIndex = i-2;
            floor.gameObject.SetActive(actualFloorIndex <= floorIndex);
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject existingPanel = GameObject.Find("FloorPanel");
        if (existingPanel != null) Destroy(existingPanel);

        GameObject floorPanel = new GameObject("FloorPanel");
        floorPanel.transform.SetParent(canvas.transform, false);
        Image panelImage = floorPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.6f, 0.9f, 0.8f);
        RectTransform rectTransform = floorPanel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 70);
        rectTransform.anchoredPosition = new Vector2(15, 410);

        GameObject textGO = new GameObject("FloorText");
        textGO.transform.SetParent(floorPanel.transform, false);
        TextMeshProUGUI floorText = textGO.AddComponent<TextMeshProUGUI>();
        floorText.text = $"Floor {floorIndex}";
        floorText.alignment = TextAlignmentOptions.Center;
        floorText.fontSize = 36;
        floorText.color = Color.white;
        RectTransform textRect = floorText.GetComponent<RectTransform>();
        textRect.sizeDelta = rectTransform.sizeDelta;
        textRect.anchoredPosition = Vector2.zero;
       //AssignJobsToDesks(floorIndex);

        Debug.Log("Selected Floor: " + floorIndex);

    }
    /*
   void AssignJobsToDesks(int floorIndex)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject existingPanel = GameObject.Find("JobCanvas");
        if (existingPanel != null) Destroy(existingPanel);



        if (gridGenerator == null) return;

        var deskAssignments = gridGenerator.deskAssignments;
        Transform selectedFloor = buildingGenerator.Find("Floor_" + floorIndex);

        // Debug Output
        foreach (var entry in deskAssignments)
        {
            Debug.Log($"Index: {entry.Key}");
            foreach (var desk in entry.Value)
            {
                Debug.Log($"  Desk {desk.deskNo} {desk.job}");
            }
        }

        if (selectedFloor != null)
        {
            Transform tablesParent = selectedFloor.Find("Tables");
            if (tablesParent != null)
            {
                foreach (var assignment in deskAssignments)
                {
                    int tableIndex = assignment.Key;
                    List<(int deskNo, string job)> deskList = assignment.Value;

                    Transform table = tablesParent.Find("Table_" + tableIndex);
                    if (table != null)
                    {
                        foreach (var (deskNo, job) in deskList)
                        {
                            // Ensure the desk exists
                            Transform desk = table.Find("Desk" + deskNo);
                            if (desk != null)
                            {
                                // Parent Canvas (World Space)
                                Canvas parentCanvas = desk.GetComponentInParent<Canvas>();
                                if (parentCanvas == null)
                                {
                                    GameObject canvasGO = new GameObject("JobCanvas");
                                    canvasGO.transform.SetParent(desk, false);
                                    parentCanvas = canvasGO.AddComponent<Canvas>();
                                    parentCanvas.renderMode = RenderMode.WorldSpace;

                                    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                                    scaler.dynamicPixelsPerUnit = 10;

                                    canvasGO.AddComponent<GraphicRaycaster>();

                                    RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
                                    canvasRect.sizeDelta = new Vector2(200, 100);
                                    parentCanvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                                }

                                // Job Panel
                                GameObject jobPanel = new GameObject("JobPanel");
                                jobPanel.transform.SetParent(parentCanvas.transform, false);

                                Image panelImage = jobPanel.AddComponent<Image>();
                                panelImage.color = new Color(0.8f, 0.8f, 0.8f, 0.9f); // Almost opaque

                                RectTransform panelRect = jobPanel.GetComponent<RectTransform>();
                                panelRect.sizeDelta = new Vector2(350, 75);
                                panelRect.localPosition = new Vector3(0, 250f, 0);  // Offset above desk
                                panelRect.localScale = Vector3.one;

                                // Apply X-axis rotation (90 degrees to face upwards)
                                panelRect.localRotation = Quaternion.Euler(90, 0, 0);

                                // Apply Y-axis rotation based on desk number (example logic)
                                if (deskNo % 2 == 0)                
                                    panelRect.Rotate(0, 0,90);     
                                else        
                                    panelRect.Rotate(0, 0, -90);    
                               
                                // Job Text
                                GameObject textGO = new GameObject("JobText");
                                textGO.transform.SetParent(jobPanel.transform, false);

                                TextMeshProUGUI jobText = textGO.AddComponent<TextMeshProUGUI>();
                                jobText.text = job;
                                jobText.alignment = TextAlignmentOptions.Center;
                                jobText.fontSize = 36;
                                jobText.color = Color.red;

                                RectTransform textRect = jobText.GetComponent<RectTransform>();
                                textRect.anchorMin = new Vector2(0, 0);
                                textRect.anchorMax = new Vector2(1, 1);
                                textRect.pivot = new Vector2(0.5f, 0.5f);
                                textRect.localPosition = Vector3.zero;
                            }


                        }
                    }
                }
            }
        }
    }*/

    private IEnumerator SmoothMoveCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        float duration = 0.5f; // Smooth transition time
        float elapsed = 0f;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRotation, t);

            yield return null;
        }

        // Final position and rotation
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
    }
    private GameObject FindInactiveObjectByName(string objectName)
    {
        Transform[] allObjects = Resources.FindObjectsOfTypeAll<Transform>(); // Find all objects (including inactive ones)
        foreach (Transform obj in allObjects)
        {
            if (obj.name == objectName)
            {
                return obj.gameObject; // Return the matching object
            }
        }
        return null; // Return null if not found
    }
}
