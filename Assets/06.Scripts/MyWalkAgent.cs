using UnityEngine;
using MLAgents;
using MLAgents.Sensors;
using System;

public class MyWalkAgent : Agent
{
    [Header("Target To Walk Towards")]
    [Space(10)]
    public Transform target;

    public Transform ground;
    public bool detectTargets;
    public bool targetIsStatic;
    public bool respawnTargetWhenTouched;
    public float targetSpawnRadius;

    [Header("Body Parts")] [Space(10)]
    public Transform body;
    public Transform leg0Upper;
    public Transform leg0Lower;
    public Transform leg1Upper;
    public Transform leg1Lower;
    public Transform leg2Upper;
    public Transform leg2Lower;

    [Header("Joint Settings")] [Space(10)] JointDriverController m_JdController;
    Vector3 m_DirToTarget;
    float m_MovingTowardsDot;
    float m_FacingDot;

    [Header("Reward Functions To Use")]
    [Space(10)]
    public bool rewardMovingTowardsTarget; // Agent should move towards target

    public bool rewardFacingTarget; // Agent should face the target
    public bool rewardUseTimePenalty; // Hurry up

    [Header("Foot Grounded Visualization")]
    [Space(10)]
    public bool useFootGroundedVisualization;

    public MeshRenderer foot0;
    public MeshRenderer foot1;
    public MeshRenderer foot2;
    public Material groundedMaterial;
    public Material unGroundedMaterial;

    Quaternion m_LookRotation;
    Matrix4x4 m_TargetDirMatrix;

    private Color originColor;

    void SetOriginColor()
    {
        //ground.GetComponent<MeshRenderer>().material.color = originColor;
    }

    public override void Initialize()
    {
        m_JdController = GetComponent<JointDriverController>();
        m_DirToTarget = target.position - body.position;
        //originColor = ground.GetComponent<MeshRenderer>().material.color;

        //Setup each body part
        m_JdController.SetupBodyPart(body);
        m_JdController.SetupBodyPart(leg0Upper);
        m_JdController.SetupBodyPart(leg0Lower);
        m_JdController.SetupBodyPart(leg1Upper);
        m_JdController.SetupBodyPart(leg1Lower);
        m_JdController.SetupBodyPart(leg2Upper);
        m_JdController.SetupBodyPart(leg2Lower);
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        var rb = bp.rb;
        sensor.AddObservation(bp.groundContact.touchingGround ? 1 : 0); // Whether the bp touching the ground

        var velocityRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(rb.velocity);
        sensor.AddObservation(velocityRelativeToLookRotationToTarget);

        var angularVelocityRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(rb.angularVelocity);
        sensor.AddObservation(angularVelocityRelativeToLookRotationToTarget);

        if (bp.rb.transform != body)
        {
            var localPosRelToBody = body.InverseTransformPoint(rb.position);
            sensor.AddObservation(localPosRelToBody);
            sensor.AddObservation(bp.currentXNormalizedRot); // Current x rot
            sensor.AddObservation(bp.currentYNormalizedRot); // Current y rot
            sensor.AddObservation(bp.currentZNormalizedRot); // Current z rot
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        m_JdController.GetCurrentJointForces();

        // Update pos to target
        m_DirToTarget = target.position - body.position;
        Debug.Log(m_DirToTarget);
        m_LookRotation = Quaternion.LookRotation(m_DirToTarget);
        m_TargetDirMatrix = Matrix4x4.TRS(Vector3.zero, m_LookRotation, Vector3.one);

        RaycastHit hit;
        if (Physics.Raycast(body.position, Vector3.down, out hit, 10.0f))
        {
            sensor.AddObservation(hit.distance);
        }
        else
            sensor.AddObservation(10.0f);

        // Forward & up to help with orientation
        var bodyForwardRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(body.forward);
        sensor.AddObservation(bodyForwardRelativeToLookRotationToTarget);

        var bodyUpRelativeToLookRotationToTarget = m_TargetDirMatrix.inverse.MultiplyVector(body.up);
        sensor.AddObservation(bodyUpRelativeToLookRotationToTarget);

        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
        //ground.GetComponent<MeshRenderer>().material.color = Color.green;
        Invoke("SetOriginColor", 0.5f);
        if (respawnTargetWhenTouched)
        {
            GetRandomTargetPos();
        }
    }

    /// <summary>
    /// Moves target to a random position within specified radius.
    /// </summary>
    public void GetRandomTargetPos()
    {
        var pos = new Vector3(UnityEngine.Random.Range(-4.5f, 4.5f)
                                          , 0.5f
                                          , UnityEngine.Random.Range(-3.0f, 3.0f));

        target.position = ground.position + pos;
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // The dictionary with all the body parts in it are in the jdController
        var bpDict = m_JdController.bodyPartsDict;

        var i = -1;
        // Pick a new target joint rotation
        bpDict[leg0Upper].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[leg1Upper].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[leg2Upper].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[leg0Lower].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[leg1Lower].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[leg2Lower].SetJointTargetRotation(vectorAction[++i], 0, 0);

        // Update joint strength
        bpDict[leg0Upper].SetJointStrength(vectorAction[++i]);
        bpDict[leg1Upper].SetJointStrength(vectorAction[++i]);
        bpDict[leg2Upper].SetJointStrength(vectorAction[++i]);
        bpDict[leg0Lower].SetJointStrength(vectorAction[++i]);
        bpDict[leg1Lower].SetJointStrength(vectorAction[++i]);
        bpDict[leg2Lower].SetJointStrength(vectorAction[++i]);

        //Vector3 dir = Vector3.zero;
        //Vector3 rot = Vector3.zero;

        //// 전진 후진 처리
        //switch ((int)vectorAction[++i])
        //{
        //    case 1: dir = bpDict[body].rb.transform.forward; break;
        //    case 2: dir = -bpDict[body].rb.transform.forward; break;
        //}

        //// 좌우 회전 
        //switch ((int)vectorAction[++i])
        //{
        //    case 1: rot = -bpDict[body].rb.transform.up; break;
        //    case 2: rot = bpDict[body].rb.transform.up; break;
        //}


        //bpDict[body].rb.AddForce(dir * moveSpeed, ForceMode.VelocityChange);
        //bpDict[body].rb.transform.Rotate(rot, Time.fixedDeltaTime * turnSpeed);
    }

    void FixedUpdate()
    {
        if (detectTargets)
        {
            foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
            {
                if (bodyPart.targetContact && bodyPart.targetContact.touchingTarget)
                {
                    TouchedTarget();
                }
            }
        }

        // If enabled the feet will light up green when the foot is grounded.
        // This is just a visualization and isn't necessary for function
        if (useFootGroundedVisualization)
        {
            foot0.material = m_JdController.bodyPartsDict[leg0Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
            foot1.material = m_JdController.bodyPartsDict[leg1Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
            foot2.material = m_JdController.bodyPartsDict[leg2Lower].groundContact.touchingGround
                ? groundedMaterial
                : unGroundedMaterial;
        }

        // Set reward for this step according to mixture of the following elements.
        if (rewardMovingTowardsTarget)
        {
            RewardFunctionMovingTowards();
        }

        if (rewardFacingTarget)
        {
            RewardFunctionFacingTarget();
        }

        if (rewardUseTimePenalty)
        {
            RewardFunctionTimePenalty();
        }

        //transform.position = m_JdController.bodyPartsDict[body].rb.position;
    }

    /// <summary>
    /// Reward moving towards target & Penalize moving away from target.
    /// </summary>
    void RewardFunctionMovingTowards()
    {
        m_MovingTowardsDot = Vector3.Dot(m_JdController.bodyPartsDict[body].rb.velocity, m_DirToTarget.normalized);
       //Debug.Log($"Moving Forward Reward : {m_MovingTowardsDot}");
        AddReward(0.03f * m_MovingTowardsDot);
    }

    /// <summary>
    /// Reward facing target & Penalize facing away from target
    /// </summary>
    void RewardFunctionFacingTarget()
    {
        m_FacingDot = Vector3.Dot(m_DirToTarget.normalized, body.forward);
        //Debug.Log($"Facing Target Reward : {m_FacingDot}");
        AddReward(0.01f * m_FacingDot);
    }

    /// <summary>
    /// Existential penalty for time-contrained tasks.
    /// </summary>
    void RewardFunctionTimePenalty()
    {
        AddReward(-0.001f);
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (m_DirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(m_DirToTarget);
        }
        transform.Rotate(Vector3.up, UnityEngine.Random.Range(0.0f, 360.0f));

        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }
        if (!targetIsStatic)
        {
            GetRandomTargetPos();
        }
    }
}
