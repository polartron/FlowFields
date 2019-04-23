using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FlowPathBehaviour : MonoBehaviour
{
    public FlowFieldSettings flowFieldSettings = new FlowFieldSettings();
    public AgentManagerSettings agentManagerSettings = new AgentManagerSettings();
    public int spawnCount = 50;
    public Transform goal;
    public Transform spawn;

    public Mesh agentMesh;
    public Material agentMaterial;
    
    
    private AgentManager agentManager;
    private FlowField field;

    void Start()
    {
        field = new FlowField(flowFieldSettings, transform.position);
        field.PopulateCost();
        field.CalculateField(goal.position);
        
        agentManager = new AgentManager(field, agentManagerSettings);
        //agentManager.Create(1, spawn.position, Quaternion.identity);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            agentManager.Create(spawnCount, spawn.position, Quaternion.identity);
        }
        
        agentManager.Update(Time.deltaTime);
        agentManager.Draw(agentMesh, agentMaterial);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            var tempField = new FlowField(flowFieldSettings, transform.position);
            tempField.PopulateCost();
            tempField.CalculateField(goal.position);
            tempField.DrawGizmos();
        }
        else
        {
            field.DrawGizmos();
        }
    }
}
