using UnityEngine;

public class CameraOrbitController : MonoBehaviour
{
    public float orbitSpeed = 10f;
    public bool autoOrbit = false;
    public float autoOrbitSpeed = 5f;
    public Vector3 orbitCenter = Vector3.zero;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    
    private float currentDistance;
    private Vector3 currentRotation;
    
    void Start()
    {
        currentDistance = Vector3.Distance(transform.position, orbitCenter);
        currentRotation = transform.eulerAngles;
    }
    
    void Update()
    {
        if (autoOrbit)
        {
            transform.RotateAround(orbitCenter, Vector3.up, autoOrbitSpeed * Time.deltaTime);
        }
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance = Mathf.Clamp(currentDistance - scroll * 5f, minDistance, maxDistance);
            UpdateCameraPosition();
        }
    }
    
    void UpdateCameraPosition()
    {
        Vector3 direction = (transform.position - orbitCenter).normalized;
        transform.position = orbitCenter + direction * currentDistance;
    }
    
    public void SetAutoOrbit(bool enabled)
    {
        autoOrbit = enabled;
    }
}