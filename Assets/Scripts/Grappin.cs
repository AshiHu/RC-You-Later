using UnityEngine;
using UnityEngine.InputSystem;

public class Grappin : MonoBehaviour
{
    [Header("Références")]
    // GLISSE TON SCRIPT DE VOITURE ICI DANS L'INSPECTEUR
    public RemoteControlCarController carController;

    public Transform gunTip;
    public LayerMask whatIsGrappable;
    private LineRenderer lr;
    private Rigidbody rb;
    private SpringJoint joint;
    private Vector3 grapplePoint;

    [Header("Réglages")]
    public float maxDistance = 100f;
    public float swingForce = 40f;
    public static bool isGrappling;
    private float originalDrag;

    void Awake()
    {
        lr = GetComponentInChildren<LineRenderer>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) StartSwing();
        if (Input.GetMouseButtonUp(0)) StopSwing();

        if (joint != null)
        {
            // 1. POUSSÉE MANUELLE
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");
            Vector3 moveInput = (transform.forward * v + transform.right * h).normalized;
            rb.AddForce(moveInput * swingForce, ForceMode.Acceleration);

            // 2. STABILISATION (Garder les roues vers le bas)
            if (rb.linearVelocity.magnitude > 1f)
            {
                Quaternion stableRot = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, stableRot, Time.deltaTime * 10f);
            }

            // 3. ANTI-VIBRATION
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    void LateUpdate() { DrawRope(); }

    void StartSwing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, whatIsGrappable))
        {
            isGrappling = true;
            grapplePoint = hit.point;

            // --- ON DÉSACTIVE LA CONDUITE ---
            if (carController != null) carController.enabled = false;

            // Configuration du Joint
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float dist = Vector3.Distance(transform.position, grapplePoint);
            joint.maxDistance = dist * 0.8f;
            joint.minDistance = dist * 0.2f;
            joint.spring = 7f;
            joint.damper = 10f;
            joint.massScale = 1f;

            lr.positionCount = 2;
        }
    }

    void StopSwing()
    {
        isGrappling = false;

        // --- ON RÉACTIVE LA CONDUITE ---
        if (carController != null) carController.enabled = true;

        lr.positionCount = 0;
        if (joint != null) Destroy(joint);
    }

    void DrawRope()
    {
        if (!joint) return;
        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, grapplePoint);
    }
}