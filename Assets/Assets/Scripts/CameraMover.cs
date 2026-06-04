using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMover : MonoBehaviour
{
    [Header("Drag")]
    [SerializeField] private float dragSensitivity = 0.02f;

    [Header("Rotation")]
    [SerializeField] private float rotationSensitivity = 0.25f;

    [Header("Inertia")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float friction = 5f;

    [Header("Floor Repulsion")]
    [SerializeField] private float repulsionStartDistance = 3f;  // how far away repulsion begins
    [SerializeField] private float repulsionStrength = 15f;

    private Vector3 lastMousePosition;
    private Vector3 currentVelocity;
    private Vector3 velocitySmoothRef;

    [Header("Zoom")]
    [SerializeField] private float zoomAcceleration = 40f;
    [SerializeField] private float zoomFriction = 8f;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 60f;

    [Header("Zoom Collision")]
    [SerializeField] private float zoomCollisionRadius = 0.5f;
    [SerializeField] private LayerMask zoomCollisionMask = ~0;

    [Header("Terrain Clearance")]
    [SerializeField] private float minHeightAboveGround = 5f;
    [SerializeField] private LayerMask terrainLayerMask = ~0;

    private Vector3 zoomVelocity;
    private bool blockZoomThisFrame = false;

    [Header("Rotation Pivot")]
    private Vector3 rotationPivot;
    private bool rotating = false;

    public bool isLocked = false;

    void Update()
    {
        if (isLocked)
        {
            currentVelocity = Vector3.zero;
            zoomVelocity = Vector3.zero;
            return;
        }

        // Prevent camera control while interacting with UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleDrag();
        HandleRotation();
        ApplyInertia();
        HandleZoom();
    }

    public void ForceStop()
    {
        currentVelocity = Vector3.zero;
        zoomVelocity = Vector3.zero;
    }

    public void BlockZoomOnce()
    {
        blockZoomThisFrame = true;
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            Vector3 right = transform.right;
            Vector3 forward = transform.forward;

            right.y = 0;
            forward.y = 0;

            right.Normalize();
            forward.Normalize();

            Vector3 targetVelocity =
                (-right * delta.x + -forward * delta.y) * dragSensitivity;

            currentVelocity = Vector3.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref velocitySmoothRef,
                smoothTime
            );

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
            rotationPivot = GetMouseGroundPosition();
            rotating = true;
        }

        if (Input.GetMouseButton(1) && rotating)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotationAmount = delta.x * rotationSensitivity;

            transform.RotateAround(rotationPivot, Vector3.up, rotationAmount);

            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            rotating = false;
        }
    }

    void ApplyInertia()
    {
        transform.position += currentVelocity;

        if (!Input.GetMouseButton(0))
        {
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                friction * Time.deltaTime
            );
        }
    }

    void HandleZoom()
    {
        if (blockZoomThisFrame) { blockZoomThisFrame = false; return; }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            float currentHeight = transform.position.y;
            float heightPercent = Mathf.InverseLerp(minZoom, maxZoom, currentHeight);
            float heightMultiplier = Mathf.Lerp(0.5f, 2f, heightPercent);
            zoomVelocity += transform.forward * scroll * zoomAcceleration * heightMultiplier;
        }

        ApplyFloorRepulsion();

        if (zoomVelocity.magnitude > 0.001f)
        {
            Vector3 moveDir = zoomVelocity.normalized;
            float moveDist = zoomVelocity.magnitude * Time.deltaTime;

            if (Physics.SphereCast(transform.position, zoomCollisionRadius,
                                   moveDir, out RaycastHit hit,
                                   moveDist + zoomCollisionRadius,
                                   zoomCollisionMask))
            {
                float safeDistance = Mathf.Max(0f, hit.distance - zoomCollisionRadius);
                transform.position += moveDir * safeDistance;
                zoomVelocity = Vector3.zero;
                return;
            }

            transform.position += zoomVelocity * Time.deltaTime;
            zoomVelocity = Vector3.Lerp(zoomVelocity, Vector3.zero, zoomFriction * Time.deltaTime);
        }

        // Hard clamp position and kill velocity AFTER all movement is done
        float floorHeight = GetMinAllowedHeight();
        if (transform.position.y < floorHeight)
        {
            transform.position = new Vector3(transform.position.x, floorHeight, transform.position.z);
            if (zoomVelocity.y < 0) zoomVelocity = Vector3.zero;
        }

        if (transform.position.y > maxZoom)
        {
            transform.position = new Vector3(transform.position.x, maxZoom, transform.position.z);
            if (zoomVelocity.y > 0) zoomVelocity = Vector3.zero;
        }
    }

    void ApplyFloorRepulsion()
    {
        float floorHeight = GetMinAllowedHeight();
        float currentHeight = transform.position.y;
        float distanceAboveFloor = currentHeight - floorHeight;

        // Gentle repulsion force only — no hard snapping here anymore
        if (distanceAboveFloor < repulsionStartDistance)
        {
            float t = 1f - Mathf.Clamp01(distanceAboveFloor / repulsionStartDistance);
            float force = t * t * repulsionStrength;
            zoomVelocity += Vector3.up * force * Time.deltaTime;
        }
    }

    Vector3 GetMouseGroundPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);

        if (ground.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }

    float GetMinAllowedHeight()
    {
        Vector3 flatForward = transform.forward;
                flatForward.y = 0f;
                flatForward.Normalize();

                Vector3 flatRight = transform.right;
                flatRight.y = 0f;
                flatRight.Normalize();

                Vector3[] sampleOffsets = new Vector3[]
                {
            Vector3.zero,
            flatForward * 10f,
            -flatForward * 10f,
            flatRight * 10f,
            -flatRight * 10f,
        };

        float highestHit = minZoom;

        foreach (Vector3 offset in sampleOffsets)
        {
            Vector3 origin = transform.position + offset + Vector3.up * 100f;
            Ray ray = new Ray(origin, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, 200f, terrainLayerMask))
            {
                float candidateMin = hit.point.y + minHeightAboveGround;
                if (candidateMin > highestHit)
                    highestHit = candidateMin;
            }
        }

        return highestHit;
    }
}