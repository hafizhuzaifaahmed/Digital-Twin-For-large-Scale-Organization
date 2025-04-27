using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;
using System.Collections;
using System.Threading.Tasks;


public class FreeAvatar : MonoBehaviour
{
    public GameObject TaskPanel;
    public GameObject JobPanel;
    public GameObject ProcessCompletedDisplay;
    TextMeshProUGUI scoreText;
    private Transform canvasTransform;
    public GameObject ScorePanel;
    public float moveSpeed = 5f; // Walking speed
    public float rotationSpeed = 0.4f; // Mouse look sensitivity
    public float gravity = 9.81f; // Gravity force

    private string job;
    private string task;
    private Transform currentDesk; // Track the current desk for job panel
    private bool jobPanelActive = false; // Track if job panel is active

    private GameObject agent;
    private Animator animator;
    private CharacterController controller;
    private Transform mainCamera;
    private GameObject score;
    private bool isMoveing= false;
    private Vector3 velocity;
    // Reference to SlideManu dictionary
    private SliderScript slideMenu;
    private Dictionary<int, List<(int deskNo, string taskName, string job, int roomNo, int floorNo, string JobCode, string taskCode)>> mappedTasks;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private int currentDeskIndex = 0; // Tracks progress through mappedTasks
    private int playerScore = 0; // Player's score
    void Start()
    {
        agent = gameObject; // Ensure reference to itself

        isMoveing = true;
        controller = GetComponent<CharacterController>();

        AttachCameraToAgent();
        GameObject canvas = GameObject.Find("Canvas");
        if(canvas != null)
        {
            canvasTransform=canvas.transform; 
        }
        slideMenu = FindObjectOfType<SliderScript>();
        if (slideMenu != null)
        {
            mappedTasks = slideMenu.mappedTasks;
            originalCameraPosition = slideMenu.originalCameraPosition;
            originalCameraRotation= slideMenu.originalCameraRotation;
        }
        else
        {
            Debug.LogError("SlideManu script not found!");
        }
        showScorePanel();

        ShowStopButton();

    }

     private void ShowStopButton()
    {
        GameObject StopButton = GameObject.Find("TestButton(Clone)");
        if (StopButton != null)
        {
            TextMeshProUGUI StopText = StopButton.GetComponentInChildren<TextMeshProUGUI>();
            if (StopText != null)
            {
                StopText.text = "Stop";
            }
        }
        Button stopBtn=StopButton.GetComponentInChildren<Button>();
        if (stopBtn != null)
        {
            stopBtn.onClick.AddListener(deleteAvatarScript);
        }
    }
    private void deleteAvatarScript()
    {
        GameObject CompleteDisplay = GameObject.Find("ProcessCompletedDisplay(Clone)");
        if (CompleteDisplay != null)
        {
            Destroy(CompleteDisplay.gameObject, 2f); // Deletes after 2 seconds
        }

        GameObject StopButton = GameObject.Find("TestButton(Clone)");
        if (StopButton != null)
        {
            Destroy(StopButton.gameObject);
        }
        GameObject Score = GameObject.Find("ScorePanel(Clone)");
        if (Score != null)
        {
            Destroy(Score.gameObject);
        }
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.transform.rotation = originalCameraRotation;
            mainCamera.transform.SetParent(null); // Unparent the camera
            Debug.Log("Camera has been detached from the agent before destruction.");
            Destroy(gameObject);
        }
        
    }
    void Update()
    {


        if (!isMoveing || agent == null)
        {
            Debug.Log("the agent is null");
            return;
        } 
        
        HandleMovement();
        HandleMouseRotation();
        ApplyGravity();
        

        if ( (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !jobPanelActive)
        {
            if (currentDesk != null)
            {
                playerScore += 1;
                Debug.Log("Enter is pressed");
                ShowJobUI(task, job); // Replace with actual job
                jobPanelActive = true;
                currentDeskIndex++;

            }
            else
            {
                playerScore -= 1;
            }
        }

        if (jobPanelActive && Input.GetKeyDown(KeyCode.LeftShift)) // 'Shift' key
        {
            HideJobUI();
            jobPanelActive = false;
        }

        if (scoreText!=null)
        {
            scoreText.text = playerScore.ToString();
        }


    }
    private void showScorePanel()
    {
        if (ScorePanel != null && canvasTransform != null)
        {
             score = Instantiate(ScorePanel, canvasTransform);

            Transform scoreTransform = score.transform.Find("Score");
            if (scoreTransform != null)
            {
                scoreText = scoreTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (scoreText != null)
                {
                    scoreText.text = playerScore.ToString();
                }
               
            }
            else
            {
                Debug.LogError("Text (TMP) not found inside Score.");
            }
        }
        else
        {
            Debug.Log("Score Panel is not found");
        }

    }
   
    private void HandleMovement()
    {
        if (agent == null)
        {
            Debug.LogError("ERROR: NavMeshAgent (agent) is NULL!");
            return;
        }

        if (controller == null)
        {
            Debug.LogError("ERROR: CharacterController (controller) is NULL!");
            return;
        }

        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxis("Vertical"); // W/S or Up/Down

        Vector3 moveDirection = agent.transform.forward * vertical + agent.transform.right * horizontal;
        moveDirection.Normalize();

        if (moveDirection.magnitude > 0)
        {
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }
    }


    private void HandleMouseRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;

        // Rotate character left/right
        agent.transform.Rotate(Vector3.up * mouseX);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            velocity.y = -2f; // Keeps player grounded
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime; // Apply gravity
        }
        controller.Move(velocity * Time.deltaTime);
    }

    private void AttachCameraToAgent()
    {
        if (Camera.main == null)
        {
            Debug.LogError("❌ Main Camera not found in the scene!");
            return;
        }

        if (agent == null)
        {
            Debug.LogError("❌ Agent is null before attaching the camera!");
            return;
        }

        mainCamera = Camera.main.transform;
        mainCamera.SetParent(agent.transform);
        mainCamera.localPosition = new Vector3(0, 1.7f, -2.5f); // Adjust camera height and distance
        mainCamera.localRotation = Quaternion.Euler(10, 0, 0); // Fixed camera angle (no up/down rotation)

        Debug.Log("📷 Camera attached to the agent!");
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collision detected: {gameObject.name} with {other.gameObject.name}");
        
        if (other.CompareTag("Desk") && gameObject.CompareTag("Player"))
        {
            Debug.Log($"Player ({gameObject.name}) touched Desk ({other.gameObject.name})");

            int deskNo;
            if (int.TryParse(other.name.Replace("Desk", ""), out deskNo))
            {
                Transform table = other.transform.parent;
                Transform floor = table?.parent?.parent;

                if (table == null || !table.name.StartsWith("Table_") || floor == null || !floor.name.StartsWith("Floor_"))
                {
                    Debug.LogWarning("⚠️ Invalid Desk/Table/Floor structure.");
                    return;
                }

                int roomNo = ExtractNumberFromName(table.name);
                int floorNo = ExtractNumberFromName(floor.name);

                if (currentDeskIndex >= mappedTasks.Count)
                {
                    Debug.Log("✅ All tasks completed!");
                    GameObject CompletedDisplay = Instantiate(ProcessCompletedDisplay, canvasTransform);
                    deleteAvatarScript();
                    return;
                }

                var expectedEntry = mappedTasks.ElementAt(currentDeskIndex).Value;
                var expectedDesk = expectedEntry.First().deskNo;
                var expectedRoom = expectedEntry.First().roomNo;
                var expectedFloor = expectedEntry.First().floorNo;

                // 🛠 Debugging: Print Actual vs. Expected values
                Debug.Log($"🔍 Checking Desk Match:\n" +
                          $"➡️ Touched Desk: {deskNo}, Expected Desk: {expectedDesk}\n" +
                          $"➡️ Touched Room: {roomNo}, Expected Room: {expectedRoom}\n" +
                          $"➡️ Touched Floor: {floorNo}, Expected Floor: {expectedFloor}");

                if (deskNo == expectedDesk && roomNo == expectedRoom && floorNo == expectedFloor)
                {
                    Debug.Log($"✅ Correct Desk! Press 'Enter' to see the job panel.");
                    job = expectedEntry.First().job;
                    task= expectedEntry.First().taskName;
                    currentDesk = other.transform; // Store the desk for later interaction
                    jobPanelActive = false;
                }
                else
                {
                    Debug.LogError("❌ Desk, Room, or Floor does NOT match expected entry!");
                }
            }

        }
    }

   
    // ✅ Extracts number from object names like "Table_0" -> 0 or "Floor_2" -> 2
    private int ExtractNumberFromName(string objectName)
    {
        int number;
        if (int.TryParse(System.Text.RegularExpressions.Regex.Match(objectName, @"\d+").Value, out number))
        {
            return number;
        }
        return -1; // Return -1 if no number found
    }



    private void ShowJobUI(string task, string job)
    {
        Canvas mainCanvas = FindObjectOfType<Canvas>();

        if (mainCanvas == null)
        {
            Debug.LogError("Main Canvas not found in the scene!");
            return;
        }

        GameObject taskUI = Instantiate(TaskPanel, mainCanvas.transform);
        if (taskUI != null)
        {
            TextMeshProUGUI taskText = taskUI.GetComponentInChildren<TextMeshProUGUI>();
            if (taskText != null)
            {
                taskText.text = task; // Completed task color
            }
        }
        GameObject JobUI = Instantiate(JobPanel, mainCanvas.transform);
        if (JobUI != null)
        {
            TextMeshProUGUI jobtext = JobUI.GetComponentInChildren<TextMeshProUGUI>();
            if (jobtext != null)
            {
                jobtext.text = job; // Completed task color
            }
        }

        currentDesk = null;
    }

    private void HideJobUI()
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
    }
}
