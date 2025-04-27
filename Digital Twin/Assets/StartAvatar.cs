using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartAvatar : MonoBehaviour
{
      
    public GameObject agentPrefab; // Assign in Inspector
    private GameObject agentInstance;

    public void SpawnAgent()
    {
        if (agentInstance != null)
        {
            Debug.LogWarning("⚠️ Agent already exists!");
            return;
        }

        // ✅ Instantiate the agent
        agentInstance = Instantiate(agentPrefab, new Vector3(0, 0, -2), Quaternion.identity);
        if (agentInstance == null)
        {
            Debug.LogError("❌ Failed to instantiate agent!");
            return;
        }

        agentInstance.tag = "Player"; // Ensure correct tag

        
    }

}
