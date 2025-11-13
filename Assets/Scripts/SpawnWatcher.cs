using UnityEngine;

public class SpawnWatcher : MonoBehaviour
{
    [Header("Prefab to detect")]
    public GameObject prefabToWatch;   // assign prefab here

    [Header("Camera to destroy")]
    public Camera cameraToDestroy;     // assign camera here

    private bool cameraDestroyed = false;

    void Update()
    {
        if (cameraDestroyed) return;
        if (prefabToWatch == null || cameraToDestroy == null) return;

        // Find all instances of this prefab in the scene
        var instances = FindObjectsOfType<GameObject>();

        foreach (var obj in instances)
        {
            // Check if it is an instance of the assigned prefab
            if (obj.name.Contains(prefabToWatch.name))
            {
                Destroy(cameraToDestroy.gameObject);
                cameraDestroyed = true;
                return;
            }
        }
    }
}
