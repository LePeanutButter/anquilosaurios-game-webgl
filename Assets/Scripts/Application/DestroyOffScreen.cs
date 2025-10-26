using UnityEngine;

public class DestroyOffScreen : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);

        if (screenPos.y < 0)
        {
            Destroy(gameObject);
        }
    }
}