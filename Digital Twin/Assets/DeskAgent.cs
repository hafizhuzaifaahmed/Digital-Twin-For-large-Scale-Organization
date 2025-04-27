using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DeskAgent : MonoBehaviour
{
    public GameObject DeskPerson;
    private SliderScript slideMenu;
    private Dictionary<int, List<(int deskNo, string taskName, string job, int roomNo, int floorNo, string JobCode, string taskCode)>> mappedTasks;

    void Start()
    {
        StartCoroutine(DelayedPlacement());
    }

    private IEnumerator DelayedPlacement()
    {
        yield return new WaitForSeconds(22f); // Wait for 15 seconds before execution

        slideMenu = FindObjectOfType<SliderScript>();
        if (slideMenu != null)
        {
            mappedTasks = slideMenu.mappedTasks;
            PlaceDeskPerson();
        }
    }

    void PlaceDeskPerson()
    {
        foreach (var taskEntry in mappedTasks)
        {
            foreach (var task in taskEntry.Value)
            {
                string floorName = $"Floor_{task.floorNo}";
                string tableName = $"Table_{task.roomNo}";
                string deskName = $"Desk{task.deskNo}";
                string chairName = $"OfficeChair{task.deskNo}";

                Transform floorTransform = GameObject.Find(floorName)?.transform;
                if (floorTransform == null) continue;

                Transform tableTransform = floorTransform.Find($"Tables/{tableName}");
                if (tableTransform == null) continue;

                Transform deskTransform = tableTransform.Find(deskName);
                if (deskTransform == null) continue;

                Transform chairTransform = tableTransform.Find(chairName);
                if (chairTransform == null)
                {
                    Debug.LogWarning($"No matching chair found for {deskName}");
                    continue;
                }

                // Instantiate and configure DeskPerson
                GameObject deskPersonInstance = Instantiate(DeskPerson, chairTransform.position, Quaternion.identity);
                deskPersonInstance.transform.SetParent(chairTransform);
                deskPersonInstance.transform.localRotation = Quaternion.Euler(0, 90, 0);
                deskPersonInstance.transform.localScale = new Vector3(1, 1.4f, 1.17f);

                // Enable Animation
                Animator animator = deskPersonInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                }

                Debug.Log($"Placed DeskPerson at {chairTransform.name} for task {task.taskName}");
            }
        }
    }
}
