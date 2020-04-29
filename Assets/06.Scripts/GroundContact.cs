using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class GroundContact : MonoBehaviour
{
    [HideInInspector] public Agent agent;

    public bool agentDoneOnGroundContact; // 땅에 닿았을 때 리셋 여부
    public bool penalizeGroundContact; // 접촉시 처벌 여부?
    public float groundContactPenalty; // 패널티 값
    public bool touchingGround;

    // 지면과 충돌여부 체크하고 선택적으로 패널티 부여
    private void OnCollisionEnter(Collision coll)
    {
        if (coll.transform.CompareTag("ground"))
        {

            touchingGround = true;
            if (penalizeGroundContact)
            {
                agent.SetReward(groundContactPenalty);
            }

            if (agentDoneOnGroundContact)
            {
                Debug.Log(coll.gameObject.name);
                //coll.transform.GetComponent<MeshRenderer>().material.color = Color.green;
                agent.EndEpisode();
            }
        }
    }

    private void OnCollisionExit(Collision coll)
    {
        if (coll.transform.CompareTag("ground"))
        {
            touchingGround = false;
           // coll.transform.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }
}
