using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class SpiderManController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] Animator animator;
    [SerializeField] Transform swingStartPoint;    
    [SerializeField] Transform swingPivot;
    [Header("Movement Parameters")]
    [SerializeField] float rotationSpeed=2.5f;
    [SerializeField] float maxSpeedX=5f;
    [SerializeField] float maxSpeedZ=5f;
    [SerializeField] float dragRunning = 0.35f;
    [Header("Swinging Parameters")]
    [SerializeField] float maxHeightToShootThread=70;
    [SerializeField] float minThreadLength=3;
    [SerializeField] float dragSwinging=0.05f;

    #region Variable and References

    Rigidbody rb;
    Transform bodyTransform;
    InputManager inputManager;
    LineRenderer lineRenderer;
    SpringJoint joint;
    Vector3 changedVelocity; 
    Vector3 swingPoint;
    Vector3 currentThreadEnd;
    
    
    float swingingInAirLayerWeight;
    float startSwingLayerWeight;
    float swingAndRunLayerWeight;


    public bool isShootingWeb;
    bool isThreadReleased;
    bool isSwinging;
    public bool onGround;


    const int swingingInAirIndex = 1;
    const int startSwingLayerIndex = 2;
    const int swingAndRunLayerIndex = 3;
    #endregion

    #region Initialization

    private void Awake()
    {
        inputManager = InputManager.Instance;
        rb = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();
        joint = GetComponent<SpringJoint>();
        bodyTransform = animator.transform;
    }

    private void OnEnable()
    {
        inputManager.swingStarted += onSwingStarted;
        inputManager.swingEnded += onSwingEnded;
    }
    private void OnDisable()
    {
        inputManager.swingStarted -= onSwingStarted;
        inputManager.swingEnded -= onSwingEnded;
    }
    private void Start()
    {
        startSwingLayerWeight = animator.GetLayerWeight(swingingInAirIndex);
        swingingInAirLayerWeight = animator.GetLayerWeight(swingingInAirIndex);
        swingAndRunLayerWeight = animator.GetLayerWeight(swingAndRunLayerIndex);
    }

    #endregion

    #region Update Loops
    private void Update()
    {
        HandleRotation();
        CheckAndBreakThread();
        AutomaticThreadShooting();
        HandleSwingRotation();
        UpdateAnimations();
    }
    public void LateUpdate()
    {
        if (isThreadReleased) DrawThread();
    }
    private void FixedUpdate()
    {
        HandleVelocity();
    }
    #endregion

    #region Movement and Rotation and Swinging
    private void HandleRotation()
    {
        transform.Rotate(new Vector3(0, 1, 0), inputManager.rotHorizontal * rotationSpeed);
    }
    private void HandleVelocity()
    {
        if (isSwinging)
        {
            rb.drag = dragSwinging;
            rb.AddForce(((swingPoint - swingStartPoint.position).normalized + transform.forward).normalized * 25, ForceMode.Acceleration);
        }
        else
        {
            rb.drag = dragRunning;
            rb.AddForce((transform.forward * inputManager.moveDirection.y + transform.right * inputManager.moveDirection.x).normalized * 15, ForceMode.Acceleration);
            ClampVelocityX();
            ClampVelocityY();           
        }
    }
    private void ClampVelocityX()
    {
        Vector3 changedVelocity = transform.InverseTransformDirection(rb.velocity);
        if (changedVelocity.x > maxSpeedX) changedVelocity = new Vector3(maxSpeedX, changedVelocity.y, changedVelocity.z);
        if (changedVelocity.x < -maxSpeedX) changedVelocity = new Vector3(-maxSpeedX, changedVelocity.y, changedVelocity.z);
        rb.velocity = transform.TransformDirection(changedVelocity);
    }
    private void ClampVelocityY()
    {
        Vector3 changedVelocity = transform.InverseTransformDirection(rb.velocity);
        if (changedVelocity.z > maxSpeedZ) changedVelocity = new Vector3(changedVelocity.x, changedVelocity.y, maxSpeedZ);
        if (changedVelocity.z < -maxSpeedZ / 2) changedVelocity = new Vector3(changedVelocity.x, changedVelocity.y, -maxSpeedZ / 2);
        rb.velocity = transform.TransformDirection(changedVelocity);
    }
    public void StartSwing()
    {

        if (!inputManager.isSwinging)
        {
            isShootingWeb = false;
            onSwingEnded();
            return;
        }
        if (!isShootingWeb) return;
       

        isShootingWeb = false;
        animator.SetBool("hasStartedSwing", false);


        autoShootInvoked = false;
        isSwinging = true;



        joint = this.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;
        float distanceFromPoint = Vector3.Distance(swingStartPoint.position, swingPivot.position);

        joint.maxDistance = distanceFromPoint * 0.7f;
        joint.minDistance = distanceFromPoint * 0.3f;

        joint.damper = 7f;
        joint.massScale = 4.5f;
        joint.spring = 4.5f;
    }
    private void HandleSwingRotation()
    {
        Quaternion targetRot = Quaternion.Euler(Vector3.zero);
        if (isSwinging && !onGround)
        {
            Vector3 threadDir = (swingPoint - transform.position).normalized;

            Vector3 planeNormalX = Vector3.Cross(transform.right, threadDir);
            Quaternion rotationX = Quaternion.LookRotation(planeNormalX);

            Vector3 planeNormalZ = Vector3.Cross(transform.forward, threadDir);
            Quaternion rotationZ = Quaternion.LookRotation(planeNormalZ);

            Vector3 eulers = new Vector3(rotationX.eulerAngles.x, 0, rotationZ.eulerAngles.x);
            targetRot = Quaternion.Euler(eulers);
        }
        bodyTransform.localRotation = Quaternion.Slerp(animator.transform.localRotation, targetRot, Time.deltaTime * 2.5f);
    }
    #endregion

    #region Thread
    bool autoShootInvoked;  

    private void AutomaticThreadShooting() 
    {
        if (inputManager.isSwinging && !isSwinging && MathF.Abs(swingStartPoint.position.y - swingPivot.position.y) > minThreadLength && transform.position.y < maxHeightToShootThread)
        {
            if (!autoShootInvoked)
            {
                autoShootInvoked = true;
                Invoke(nameof(onSwingStarted), 0.5f);
            }
        }
        else 
        {
            CancelInvoke(nameof(onSwingStarted));
        }
       
    }
    private void CheckAndBreakThread() 
    {
        if (isSwinging && MathF.Abs(swingStartPoint.position.y - swingPoint.y) < minThreadLength || transform.position.y > maxHeightToShootThread)
        {
            onSwingEnded();
        }       
    }

    public void ReleaseThread()
    {
        if (!isShootingWeb) return;

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;
        isThreadReleased = true;
        swingPoint = swingPivot.position;
        currentThreadEnd = swingStartPoint.position;
    }

    private void DrawThread() 
    {
        currentThreadEnd = Vector3.MoveTowards(currentThreadEnd, swingPoint, Time.deltaTime * 75);
        lineRenderer.SetPositions(new Vector3[] { swingStartPoint.position,currentThreadEnd});
    }
    #endregion

    #region Animations
    private void StartSwingAnimation() 
    {
        isShootingWeb = true;
        //animator.SetLayerWeight(startSwingLayerIndex, 1);
        animator.SetBool("hasStartedSwing", true);
    }
    private void UpdateLayerWeights() 
    {
        bool falling = !onGround && !isSwinging;
        bool swing = isSwinging;
        bool swingAndRun = onGround && isSwinging;

       

        swingAndRunLayerWeight = Mathf.Lerp(swingAndRunLayerWeight,swingAndRun ? 1 : 0, Time.deltaTime*5);
        swingingInAirLayerWeight = Mathf.Lerp(swingingInAirLayerWeight, swing || falling ? 1 : 0, Time.deltaTime*5);
        startSwingLayerWeight = Mathf.Lerp(startSwingLayerWeight, isShootingWeb ? 1 : 0, Time.deltaTime * 3.5f);

        animator.SetLayerWeight(swingingInAirIndex, swingingInAirLayerWeight);
        animator.SetLayerWeight(swingAndRunLayerIndex, swingAndRunLayerWeight);
        animator.SetLayerWeight(startSwingLayerIndex, startSwingLayerWeight);

        animator.SetBool("isFalling", !onGround && !isSwinging);
        animator.SetBool("onGround", onGround);
        animator.SetBool("isSwinging", isSwinging);       
    }
    private void UpdateAnimations() 
    {
        UpdateLayerWeights();
        Vector3 changedVelocity = transform.InverseTransformDirection (rb.velocity);
        animator.SetFloat("X",changedVelocity.x/maxSpeedX);
        animator.SetFloat("Y",changedVelocity.z/maxSpeedZ);
    }
    #endregion

    #region CallBacks
    private void onSwingStarted()
    {
        StartSwingAnimation();
    }
    private void onSwingEnded()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
        Destroy(joint);
        isSwinging = false;
        isShootingWeb = false;
        isThreadReleased = false;
        animator.SetBool("hasStartedSwing", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Ground")
            onGround = true;

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ground")
            onGround = false;
    }
    #endregion
}
