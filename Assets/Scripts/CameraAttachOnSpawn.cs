using UnityEngine;

public class CameraAttachOnSpawn : MonoBehaviour
{
    [Header("Camera to attach")]
    public Camera cameraToAttach;

    [Header("Prefab to detect")]
    public GameObject prefabToWatch;

    private bool attached = false;

    void Update()
    {
        if (attached) return;
        if (cameraToAttach == null || prefabToWatch == null) return;

        GameObject[] objects = FindObjectsOfType<GameObject>();

        foreach (var obj in objects)
        {
            // Detect prefab instance by name
            if (obj.name.Contains(prefabToWatch.name))
            {
                // Make camera a child of the prefab instance
                cameraToAttach.transform.SetParent(obj.transform, true);

                // Set ONLY the X to 0
                Vector3 localPos = cameraToAttach.transform.localPosition;
                localPos.x = 0;
                cameraToAttach.transform.localPosition = localPos;

                attached = true;
                return;
            }
        }
    }
}
