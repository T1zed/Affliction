using UnityEngine;

public class CameraFollow2_5D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public float offsetX = 0f;
    public float offsetY = 2f;
    public float offsetZ = -10f;

    [Header("Smooth Settings")]
    public float smoothTimeX = 0.3f; 
    public float smoothTimeY = 0.15f; 

    [Header("Bounds (optionnel)")]
    public bool useBounds = false;
    public float minX = -50f, maxX = 50f;
    public float minY = -10f, maxY = 20f;

    private float _velocityX = 0f; 
    private float _velocityY = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        float desiredX = target.position.x + offsetX;
        float desiredY = target.position.y + offsetY;

        if (useBounds)
        {
            desiredX = Mathf.Clamp(desiredX, minX, maxX);
            desiredY = Mathf.Clamp(desiredY, minY, maxY);
        }

        float newX = Mathf.SmoothDamp(transform.position.x, desiredX, ref _velocityX, smoothTimeX);
        float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref _velocityY, smoothTimeY);

        transform.position = new Vector3(newX, newY, offsetZ);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        transform.position = new Vector3(newTarget.position.x + offsetX,newTarget.position.y + offsetY,offsetZ);
        _velocityX = 0f;
        _velocityY = 0f; 
    }
}