using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class RemoteControlCarController : MonoBehaviour
{
    [SerializeField]
    private AnimationCurve motorPowerOverForwardSpeed = new(
        new Keyframe(0.0f, 4.0f),
        new Keyframe(10.0f, 10.0f),
        new Keyframe(20.0f, 0.0f)
        );

    [SerializeField]
    private float breakForce = 10.0f;

    /// <summary>
    /// https://docs.unity3d.com/2023.2/Documentation//ScriptReference/WheelFrictionCurve.html
    /// </summary>
    [SerializeField]
    private AnimationCurve sidewaysFriction = new(
        new Keyframe(0.0f, 0.0f),
        new Keyframe(0.2f, 1.0f),
        new Keyframe(0.5f, 0.75f)
        );

    [SerializeField]
    private float stiffness = 10f;

    [SerializeField]
    private float frontWheelMaxAngle = 45.0f;

    [SerializeField]
    private float minForwardSpeedForRotation = 1.0f;

    [SerializeField]
    private float airRotationControl = 0.4f;

    [SerializeField]
    private Transform modelPivot = null;

    [SerializeField]
    private Transform frontLeftWheel = null;

    [SerializeField]
    private Transform frontRightWheel = null;

    [SerializeField]
    private GameObject[] groundParticles = new GameObject[0];

    private InputAction playerMoveAction = null;
    private SphereCollider sphereCollider = null;
    private new Rigidbody rigidbody = null;

    /// <summary>
    /// Surface's normal below this threshold will be treated as ground (1: all, 0: half, -1: none).
    /// Default is -0.25f, meaning up until 67.5°.
    /// </summary>

    private float gravityGroundThreshold = -0.25f;
    private Vector3 averageGroundNormal = Vector3.up;
    private Vector3 previousLinearVelocity = Vector3.zero;
    private float maxRotRadPerSec = 180.0f * Mathf.Deg2Rad;

    private Dictionary<Collider, List<GroundContactPoint>> groundContactPoints = new Dictionary<Collider, List<GroundContactPoint>>(8);
    private float groundContactGracePeriod = 0.2f;
    private float lastGroundContactPointTime = 0.0f;

    public Vector3 GravityDirection => Physics.gravity.normalized;
    public bool IsGrounded => groundContactPoints.Count > 0 || (Time.fixedTime - lastGroundContactPointTime) < groundContactGracePeriod;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Vector3 ForwardDirection { get; private set; } = Vector3.forward;
    public Vector3 RightDirection { get; private set; } = Vector3.right;

    private void Awake()
    {
        GetComponentsIFN();
        rigidbody.sleepThreshold = 0.0f;
        GroundNormal = averageGroundNormal = -GravityDirection;
        ForwardDirection = transform.forward;
    }

    private void Start()
    {
        // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputActionAsset.html
        // Player/Move
        playerMoveAction = InputSystem.actions.FindAction("351f2ccd-1f9f-44bf-9bec-d62ac5c5f408", true);
    }

    private void GetComponentsIFN()
    {
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 linearVelocity = rigidbody.linearVelocity;
        // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/Workflow-Actions.html
        Vector2 playerMoveInput = playerMoveAction.ReadValue<Vector2>();
        float angle = playerMoveInput.x * frontWheelMaxAngle;

        if (IsGrounded)
        {
            // Compute the average ground normal based on our ground contact point(s).
            averageGroundNormal = ComputeAverageGroundNormal(averageGroundNormal);
            // Rotate our ground's normal direction toward the average ground normal.
            GroundNormal = Vector3.RotateTowards(GroundNormal, averageGroundNormal, maxRotRadPerSec * Time.fixedDeltaTime, 0f).normalized;

            // Project our direction's vector onto the ground plane and re-normalized it.
            // https://docs.unity3d.com/2020.3/Documentation//ScriptReference/Vector3.ProjectOnPlane.html

            Vector3 groundProjectedForwardDirection = Vector3.ProjectOnPlane(ForwardDirection, GroundNormal).normalized;

            // Rotate our forward direction vector toward the projected version.
            ForwardDirection = Vector3.RotateTowards(ForwardDirection, groundProjectedForwardDirection, maxRotRadPerSec * Time.fixedDeltaTime, 0f).normalized;

            // Compute the forward component of our velocity.
            float forwardVelocity = Vector3.Dot(ForwardDirection, linearVelocity);

            // Rotate our forward vector based on player's input.
            if (Mathf.Abs(playerMoveInput.x) > 0.01f)
            {
                float angleOverSpeedRatio = 1.0f;
                float absForwardVelocity = Mathf.Abs(forwardVelocity);
                if (absForwardVelocity < minForwardSpeedForRotation) angleOverSpeedRatio = absForwardVelocity / minForwardSpeedForRotation;
                
                // Create a Quaternion that will store the rotation we want to apply.
                Quaternion rotation = Quaternion.AngleAxis(angle * angleOverSpeedRatio * Time.fixedDeltaTime, GroundNormal);
                ForwardDirection = rotation * ForwardDirection;
            }
            
            // Compute our right vector using our forward and (reversed) ground normal.
            // https://docs.unity3d.com/2020.3/Documentation//ScriptReference/Vector3.Cross.html
            RightDirection = Vector3.Cross(ForwardDirection, -GroundNormal).normalized;

            // Sideways Friction //

            // How much our velocity is slipping velocity?
            // 0    means 100% of our velocity is perpendicular to our forward direction (100% sliping).
            // -1/1 means 0% is perpendicular and thus 100% parallel (no sliping).
            float slip = Vector3.Dot(RightDirection, linearVelocity.normalized);
            float friction = sidewaysFriction.Evaluate(Mathf.Abs(slip));
            float frictionForce = friction * stiffness * -Mathf.Sign(slip);

            rigidbody.AddForce(frictionForce * RightDirection);

            // Motor Force //
            if (playerMoveInput.y > 0.01f)
            {
                float motorForce = motorPowerOverForwardSpeed.Evaluate(forwardVelocity) * playerMoveInput.y;
                rigidbody.AddForce(motorForce * ForwardDirection);
            }
            else
            {
                // Breaking Force/Motor //
                float breakingForce = 0.0f;
                float breakingDirection = -Mathf.Sign(Vector3.Dot(ForwardDirection, linearVelocity));

                if (playerMoveInput.y < -0.01f) breakingForce = breakForce * Mathf.Abs(playerMoveInput.y);
                else breakingForce = 1.0f + breakForce * 0.1f;

                rigidbody.AddForce(breakingForce * breakingDirection * ForwardDirection);
            }
        }
        else
        {
            // NOT Grounded
            averageGroundNormal = -GravityDirection;
            Quaternion rigidbodyRotation = Quaternion.FromToRotation(previousLinearVelocity.normalized, rigidbody.linearVelocity.normalized);

            GroundNormal = rigidbodyRotation * GroundNormal;
            ForwardDirection = rigidbodyRotation * ForwardDirection;
            RightDirection = rigidbodyRotation * RightDirection;

            if (Mathf.Abs(playerMoveInput.x) > 0.01f)
            {
                float forwardVelocity = Vector3.Dot(ForwardDirection, linearVelocity);
                float angleOverSpeedRatio = 1.0f;
                float absForwardVelocity = Mathf.Abs(forwardVelocity);
                if (absForwardVelocity < minForwardSpeedForRotation) angleOverSpeedRatio = absForwardVelocity / minForwardSpeedForRotation;

                // Create a Quaternion that will store the rotation we want to apply.
                Quaternion rotation = Quaternion.AngleAxis(angle * angleOverSpeedRatio * airRotationControl * Time.fixedDeltaTime, GroundNormal);
                ForwardDirection = rotation * ForwardDirection;

                // Recompute our right vector using our forward and (reversed) ground normal.
                // https://docs.unity3d.com/2020.3/Documentation//ScriptReference/Vector3.Cross.html
                RightDirection = Vector3.Cross(ForwardDirection, -GroundNormal).normalized;
            }
        }
        previousLinearVelocity = rigidbody.linearVelocity;
    }

    private void Update()
    {
        modelPivot.position = transform.position + -GroundNormal * sphereCollider.radius;
        modelPivot.LookAt(modelPivot.position + ForwardDirection, GroundNormal);

        Vector2 playerMoveInput = playerMoveAction.ReadValue<Vector2>();
        float wheelAngle = playerMoveInput.x * frontWheelMaxAngle;

        frontLeftWheel.localEulerAngles = Vector3.up * wheelAngle;
        frontRightWheel.localEulerAngles = Vector3.up * wheelAngle;

        if (groundParticles != null && groundParticles.Length > 0)
        {
            for (int index = 0; index < groundParticles.Length; index++)
            {
                groundParticles[index].SetActive(IsGrounded);
            }
        }
    }

    // --- Les fonctions de contact et Gizmos restent identiques ---
    private Vector3 ComputeAverageGroundNormal(Vector3 previousAverageGroundNormal)
    {
        Vector3 averageGroundNormal = Vector3.zero;
        int groundContactCount = 0;
        foreach (List<GroundContactPoint> points in this.groundContactPoints.Values)
        {
            foreach (GroundContactPoint point in points)
            {
                averageGroundNormal += point.Normal;
                groundContactCount++;
            }
        }
        if (groundContactCount == 0) return previousAverageGroundNormal;
        return averageGroundNormal / (float)groundContactCount;
    }

    private void UpdateGroundContact(Collision collision, bool remove = false)
    {
        Collider collider = collision.collider;
        if (remove)
        {
            groundContactPoints.Remove(collider);
            if (groundContactPoints.Count == 0) lastGroundContactPointTime = Time.fixedTime;
        }
        else
        {
            if (!groundContactPoints.ContainsKey(collider)) groundContactPoints.Add(collider, new List<GroundContactPoint>(4));
            else groundContactPoints[collider].Clear();

            List<GroundContactPoint> points = groundContactPoints[collider];
            ContactPoint[] contacts = new ContactPoint[collision.contactCount];
            collision.GetContacts(contacts);

            for (int i = 0; i < contacts.Length; i++)
            {
                // Does this contact point should be considered as ground?
                // First let's retrieve the surface normal.
                // N.b.: ContactPoint.normal represent the "average" normal between the two collider's surface's normal.

                // https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider.Raycast.html
                Ray ray = new(transform.position, (contacts[i].point - transform.position).normalized);
                if (collider.Raycast(ray, out RaycastHit hitInfo, float.MaxValue))
                {
                    if (Vector3.Dot(hitInfo.normal, GravityDirection) < gravityGroundThreshold)
                    {
                        points.Add(new GroundContactPoint(contacts[i].point, hitInfo.normal));
                    }
                }
            }
        }
    }

    private void OnCollisionEnter(Collision c) => UpdateGroundContact(c);
    private void OnCollisionStay(Collision c) => UpdateGroundContact(c);
    private void OnCollisionExit(Collision c) => UpdateGroundContact(c, true);

    private struct GroundContactPoint
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public GroundContactPoint(Vector3 p, Vector3 n) { Point = p; Normal = n; }
    }
}