using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using MLAgents;

public class BodyPart
{
    [Header("Body Part Info")] [Space(10)]
    public ConfigurableJoint joint;
    public Rigidbody rb;
    [HideInInspector] public Vector3 startingPos;
    [HideInInspector] public Quaternion startingRot;

    public GroundContact groundContact;

    public TargetContact targetContact;

    [HideInInspector] public JointDriverController thisJdController;

    public Vector3 currentEularJointRotation;

    [HideInInspector] public float currentStrength;
    public float currentXNormalizedRot;
    public float currentYNormalizedRot;
    public float currentZNormalizedRot;

    public Vector3 currentJointForce;

    public float currentJointForceSqrMag;
    public Vector3 currentJointTorque;
    public float currentJointTorqueSqrMag;
    public AnimationCurve jointForceCurve = new AnimationCurve();
    public AnimationCurve jointTouqueCurve = new AnimationCurve();

    // 각 신체 파트를 기본값으로 초기화
    public void Reset(BodyPart bp)
    {
        bp.rb.transform.position = bp.startingPos;
        bp.rb.transform.rotation = bp.startingRot;
        bp.rb.velocity = Vector3.zero;
        bp.rb.angularVelocity = Vector3.zero;

        if (bp.groundContact)
        {
            bp.groundContact.touchingGround = false;
        }

        if (bp.targetContact)
        {
            bp.targetContact.touchingTarget = false;
        }
    }

    // 정의된 x, y, z 값에 따라 touque값을 적용하고 strength를 가한다
    public void SetJointTargetRotation(float x, float y, float z)
    {
        x = (x + 1.0f) * 0.5f;
        y = (y + 1.0f) * 0.5f;
        z = (z + 1.0f) * 0.5f;

        var xRot = Mathf.Lerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, x);
        var yRot = Mathf.Lerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, y);
        var zRot = Mathf.Lerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, z);

        currentXNormalizedRot = Mathf.InverseLerp(joint.lowAngularXLimit.limit, joint.highAngularXLimit.limit, xRot);
        currentYNormalizedRot = Mathf.InverseLerp(-joint.angularYLimit.limit, joint.angularYLimit.limit, yRot);
        currentZNormalizedRot = Mathf.InverseLerp(-joint.angularZLimit.limit, joint.angularZLimit.limit, zRot);

        joint.targetRotation = Quaternion.Euler(xRot, yRot, zRot);
        currentEularJointRotation = new Vector3(xRot, yRot, zRot);
    }

    public void SetJointStrength(float strenth)
    {
        var rawVal = (strenth + 1.0f) * 0.5f * thisJdController.maxJointForceLimit;
        var jd = new JointDrive
        {
            positionSpring = thisJdController.maxJointSpring,
            positionDamper = thisJdController.jointDamper,
            maximumForce = rawVal
        };
        joint.slerpDrive = jd;
        currentStrength = jd.maximumForce;
    }
}

public class JointDriverController : MonoBehaviour
{
    public float maxJointSpring;

    public float jointDamper;
    public float maxJointForceLimit;
    float FacingDot;

    [HideInInspector] public Dictionary<Transform, BodyPart> bodyPartsDict = new Dictionary<Transform, BodyPart>();

    [HideInInspector] public List<BodyPart> bodyPartsList = new List<BodyPart>();

    // BodyPart 오브젝트 생성 및 dictionary에 추가 
    public void SetupBodyPart(Transform t)
    {
        var bp = new BodyPart
        {
            rb = t.GetComponent<Rigidbody>(),
            joint = t.GetComponent<ConfigurableJoint>(),
            startingPos = t.position,
            startingRot = t.rotation
        };
        bp.rb.maxAngularVelocity = 100.0f;

        // groundContact Script 추가 및 초기화
        bp.groundContact = t.GetComponent<GroundContact>();
        if (!bp.groundContact)
        {
            bp.groundContact = t.gameObject.AddComponent<GroundContact>();
            bp.groundContact.agent = gameObject.GetComponent<Agent>();
        }
        else
        {
            bp.groundContact.agent = gameObject.GetComponent<Agent>();
        }

        // targetContact Script 추가 및 초기화
        bp.targetContact = t.GetComponent<TargetContact>();
        if (!bp.targetContact)
        {
            bp.targetContact = t.gameObject.AddComponent<TargetContact>();
        }

        bp.thisJdController = this;
        bodyPartsDict.Add(t, bp);
        bodyPartsList.Add(bp);
    }

    public void GetCurrentJointForces()
    {
        foreach(var bodyPart in bodyPartsDict.Values)
        {
            if (bodyPart.joint)
            {
                bodyPart.currentJointForce = bodyPart.joint.currentForce;
                bodyPart.currentJointForceSqrMag = bodyPart.joint.currentForce.magnitude;
                bodyPart.currentJointTorque = bodyPart.joint.currentTorque;
                bodyPart.currentJointTorqueSqrMag = bodyPart.joint.currentTorque.magnitude;

                if (Application.isEditor)
                {
                    if(bodyPart.jointForceCurve.length > 1000)
                    {
                        bodyPart.jointForceCurve = new AnimationCurve();
                    }

                    if (bodyPart.jointTouqueCurve.length > 1000)
                    {
                        bodyPart.jointTouqueCurve = new AnimationCurve();
                    }

                    bodyPart.jointForceCurve.AddKey(Time.time, bodyPart.currentJointForceSqrMag);
                    bodyPart.jointTouqueCurve.AddKey(Time.time, bodyPart.currentJointTorqueSqrMag);

                }
            }
        }
    }
}

