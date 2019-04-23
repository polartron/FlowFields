using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Agent
{
    public int Identifier;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector2 Velocity;

    public Vector2Int GridPosition;
    public Vector3 Flow;

    public float SurfaceFriction = 1f;
}

[Serializable]
public class AgentManagerSettings
{
    public float MaxSpeed = 6f;
    public float StopSpeed = 1.905f;
    public float Friction = 4f;
}

public class AgentManager
{
    private List<Agent> agents = new List<Agent>();
    private FlowField flowField;
    private AgentManagerSettings settings;

    public AgentManager(FlowField flowField, AgentManagerSettings settings)
    {
        this.flowField = flowField;
        this.settings = settings;
    }

    public void Create(int amount, Vector3 position, Quaternion rotation)
    {
        for (int i = 0; i < amount; i++)
        {
            agents.Add(new Agent()
            {
                Position = position,
                Rotation = rotation,
                Velocity = Vector3.zero,
                Identifier = agents.Count //Change this to use incremental value
            });
        }
    }

    public void Update(float deltaTime)
    {
        int count = agents.Count;
        for (int i = 0; i < count; i++)
        {
            var agent = agents[i];

            var gridCoordinate = flowField.WorldToGridCoordinate(agent.Position);

            if (gridCoordinate != agent.GridPosition)
            {
                agent.Flow = flowField.GetVector(gridCoordinate) ?? agent.Flow;
                agent.GridPosition = gridCoordinate;
            }

            SimpleMove(agent, agent.Flow, settings.MaxSpeed, deltaTime);
        }
    }

    public void Draw(Mesh mesh, Material material)
    {
        for (int i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            Graphics.DrawMesh(mesh, agent.Position + Vector3.up, Quaternion.identity, material, 0);
        }
    }

    private void SimpleMove(Agent agent, Vector3 direction, float speed, float deltaTime)
    {
        // TODO: Use cells or quadtrees instead of this worst case scenario
        List<Agent> neighbours = new List<Agent>();
        for (int i = 0; i < agents.Count; i++)
        {
            if (Vector3.Distance(agent.Position, agents[i].Position) < 2f)
            {
                if (agent.Identifier != agents[i].Identifier)
                {
                    neighbours.Add(agents[i]);
                }
            }
        }
        
        agent.Velocity = Vector2.Lerp(agent.Velocity, direction * speed, deltaTime);

        Avoid(agent, neighbours, 2, deltaTime * 15f);
        
        Friction(ref agent.Velocity, settings.StopSpeed, settings.Friction * agent.SurfaceFriction, deltaTime);
        
        agent.Position.x += agent.Velocity.x * deltaTime;
        agent.Position.z += agent.Velocity.y * deltaTime;
        //TODO: Smooth y based on field height. We don't want to sample navmesh position (costly)
    }
  
    private void Avoid(Agent agent, List<Agent> neighbours, float avoidanceRadius, float delta)
    {
        Vector2 avoidance = Vector2.zero;
        int avoidCount = 0;

        foreach (var neighbour in neighbours)
        {
            float dot = Vector2.Dot(neighbour.Velocity, agent.Velocity);
            
            if (dot > 0 && Vector3.Distance(neighbour.Position, agent.Position) < avoidanceRadius)
            {
                avoidCount++;
                var offset = agent.Position - neighbour.Position;
                avoidance.x += offset.x;
                avoidance.y += offset.z;
            }
        }

        if (avoidCount > 0)
        {
            avoidance /= avoidCount;
        }

        agent.Velocity += avoidance * delta;
    }
    
    //https://github.com/AwesomeX/Fragsurf-Character-Controller/blob/master/Source/Fragsurf/Movement/SurfPhysics.cs
    public static void Friction(ref Vector2 velocity, float stopSpeed, float friction, float deltaTime)
    {
        var speed = velocity.magnitude;

        if (speed < 0.0001905f)
        {
            return;
        }

        var drop = 0f;

        // apply ground friction
        var control = (speed < stopSpeed) ? stopSpeed : speed;
        drop += control * friction * deltaTime;

        // scale the velocity
        var newspeed = speed - drop;

        if (newspeed < 0)
            newspeed = 0;

        if (newspeed != speed)
        {
            newspeed /= speed;
            velocity *= newspeed;
        }
    }
}