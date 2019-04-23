using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


[Serializable]
public class FlowFieldSettings
{
    public int Width = 50;
    public int Height = 50;
    public float CellSize = 1f;
}

public class FlowFieldData
{
    public byte[] Cost;
    public short[] Integration;
    public byte[] Flow;
    public float[] Height;
}

[Serializable]
public class FlowField
{
    private FlowFieldSettings settings;
    private Vector3 origin;
    private FlowFieldData data;

    private Vector2Int[] neighbours =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
    };

    private Vector2Int[] directions =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),

        new Vector2Int(1, 0),
        new Vector2Int(1, -1),

        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),

        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
    };

    List<Vector2Int> openNodes = new List<Vector2Int>(1000);

    public FlowField(FlowFieldSettings settings, Vector3 origin)
    {
        this.settings = settings;
        this.origin = origin;

        data = new FlowFieldData();
        data.Cost = new byte[settings.Width * settings.Height];
        data.Integration = new short[settings.Width * settings.Height];
        data.Flow = new byte[settings.Width * settings.Height];
        data.Height = new float[settings.Width * settings.Height];
    }

    public Vector2Int WorldToGridCoordinate(Vector3 position)
    {
        Vector3 offset = position - origin;

        float halfCellSize = settings.CellSize / 2;
        
        float x = (offset.x + halfCellSize) / settings.CellSize;
        float y = (offset.z + halfCellSize) / settings.CellSize;
        
        int gridOffsetX = (int) (x + short.MaxValue) - short.MaxValue;
        int gridOffsetY = (int) (y + short.MaxValue) - short.MaxValue;

        return new Vector2Int(settings.Width / 2 + gridOffsetX, settings.Height / 2 + gridOffsetY);
    }

    private Vector3 GridToWorldPosition(Vector2Int gridCoordinate)
    {
        float cellSize = settings.CellSize;
        float halfWidth = (settings.Width * cellSize) / 2f;
        float halfHeight = (settings.Height * cellSize) / 2f;
        return origin + new Vector3(-halfWidth + gridCoordinate.x * cellSize, 0f,
            -halfHeight + gridCoordinate.y * cellSize);
    }

    private int ArrayIndexFromGridCoordinate(Vector2Int gridCoordinate)
    {
        return gridCoordinate.x + settings.Width * gridCoordinate.y;
    }

    private Vector2Int GridCoordinateFromArrayIndexFrom(int index)
    {
        int width = settings.Width;
        return new Vector2Int(index % width, index / width);
    }

    private bool CoordinateInsideGrid(Vector2Int gridCoordinate)
    {
        return !(gridCoordinate.x >= settings.Width
            || gridCoordinate.x < 0
            || gridCoordinate.y >= settings.Height
            || gridCoordinate.y < 0);
    }

    public Vector2? GetVector(Vector2Int gridCoordinate)
    {
        int index = ArrayIndexFromGridCoordinate(gridCoordinate);

        if (index < 0 || index >= data.Flow.Length)
        {
            return null;
        }

        var flow = data.Flow[index];

        if (flow >= directions.Length)
        {
            return null;
        }

        return directions[flow];
    }

    public void CalculateField(Vector3 goal)
    {
        for (int i = 0; i < data.Integration.Length; i++)
        {
            data.Integration[i] = short.MaxValue;
        }

        Vector2Int gridGoal = WorldToGridCoordinate(goal);
        int goalIndex = ArrayIndexFromGridCoordinate(gridGoal);

        openNodes.Clear();
        openNodes.Add(gridGoal);

        data.Integration[goalIndex] = 0;
        data.Flow[goalIndex] = 0;

        while (openNodes.Count > 0)
        {
            Vector2Int current = openNodes[0];
            openNodes.RemoveAt(0);

            int currentIndex = ArrayIndexFromGridCoordinate(current);
            short currentCost = data.Integration[currentIndex];

            foreach (var neighbour in neighbours)
            {
                Vector2Int neighbourCoordinate = current + neighbour;

                int index = ArrayIndexFromGridCoordinate(neighbourCoordinate);

                if (!CoordinateInsideGrid(neighbourCoordinate) || data.Cost[index] == 0)
                {
                    continue;
                }

                short tileCost = Convert.ToInt16(data.Cost[index] + currentCost);

                if (data.Integration[index] > tileCost)
                {
                    openNodes.Add(neighbourCoordinate);
                    data.Integration[index] = tileCost;
                }
            }
        }

        data.Flow[goalIndex] = 0;

        for (int i = 0; i < settings.Height * settings.Width; i++)
        {
            if (data.Integration[i] == short.MaxValue || i == goalIndex)
            {
                continue;
            }

            short currentCost = data.Integration[i];
            Vector2Int current = GridCoordinateFromArrayIndexFrom(i);

            byte lowestIndex = 0;
            short lowestCost = 0;

            for (byte n = 0; n < directions.Length; n++)
            {
                Vector2Int direction = directions[n];

                int index = ArrayIndexFromGridCoordinate(current + direction);

                if (index < 0 || index >= data.Integration.Length)
                {
                    continue;
                }

                short nextCost = data.Integration[index];
                short difference = (short) (nextCost - currentCost);

                if (difference < lowestCost)
                {
                    lowestIndex = n;
                    lowestCost = difference;
                }
            }
            
            data.Flow[i] = lowestIndex;

            // Fix for rounding corners where the direction would point towards the corner tile
            
            if (lowestIndex > 0 && lowestIndex % 2 != 0)
            {
                int length = directions.Length;

                int directionIndex1 = (lowestIndex + 1 % length + length) % length;
                int directionIndex2 = (lowestIndex - 1 % length + length) % length;
                
                Vector2Int o1 = directions[directionIndex1];
                Vector2Int o2 = directions[directionIndex2];

                int index1 = ArrayIndexFromGridCoordinate(current + o1);
                int index2 = ArrayIndexFromGridCoordinate(current + o2);
                
                if (index1 > 0 || index1 < data.Cost.Length)
                {
                    int cost = data.Cost[ArrayIndexFromGridCoordinate(current + o1)];
                    
                    if (cost == 255 || cost == 0)
                    {
                        data.Flow[i] = (byte) directionIndex2;
                    }
                }
                
                if (index2 > 0 || index2 < data.Cost.Length)
                {
                    int cost = data.Cost[ArrayIndexFromGridCoordinate(current + o2)];
                    
                    if (cost == 255 || cost == 0)
                    {
                        data.Flow[i] = (byte) directionIndex1;
                    }
                }
            }
        }
    }

    public void PopulateCost()
    {
        for (int i = 0; i < settings.Width * settings.Height; i++)
        {
            data.Cost[i] = 0;
        }

        Vector2Int middle = new Vector2Int(settings.Width / 2, settings.Height / 2);
        openNodes.Clear();
        openNodes.Add(middle);

        while (openNodes.Count > 0)
        {
            Vector2Int current = openNodes[0];
            openNodes.RemoveAt(0);

            int tileIndex = current.x + settings.Width * current.y;

            if (!CoordinateInsideGrid(current))
            {
                continue;
            }

            if (data.Cost[tileIndex] > 0)
            {
                continue;
            }

            Vector3 position = GridToWorldPosition(current);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 20f, NavMesh.AllAreas))
            {
                Vector3 hitPosition = hit.position;

                if (Mathf.Approximately(hitPosition.x, position.x) &&
                    Mathf.Approximately(hitPosition.z, position.z))
                {
                    data.Height[tileIndex] = hitPosition.y;
                    data.Cost[tileIndex] = 1;
                }
                else
                {
                    data.Cost[tileIndex] = 255;
                    continue;
                }
            }
            else
            {
                data.Cost[tileIndex] = 255;
                continue;
            }

            for (int i = 0; i < neighbours.Length; i++)
            {
                Vector2Int gridPosition = current + neighbours[i];

                int index = ArrayIndexFromGridCoordinate(gridPosition);
                
                if (index < 0 || index >= data.Cost.Length)
                {
                    continue;
                }
                
                if (data.Cost[index] == 0)
                {
                    openNodes.Add(gridPosition);
                }
            }
        }
    }

    public void DrawGizmos()
    {
        if (data == null)
        {
            return;
        }

        for (var i = 0; i < data.Flow.Length; i++)
        {
            var integration = data.Integration[i];

            if (integration == short.MaxValue || data.Cost[i] == 0)
            {
                continue;
            }

            var color = Gizmos.color;

            Gizmos.color = Color.HSVToRGB(integration / 200f, 1, 1);

            Vector3 position = GridToWorldPosition(GridCoordinateFromArrayIndexFrom(i));
            position.y = data.Height[i];

            Gizmos.DrawCube(position, new Vector3(settings.CellSize, 0.1f, settings.CellSize));

            var flow = data.Flow[i];

            Color c = Color.HSVToRGB(flow / 9f, 1, 1);

            Vector3 target = GridToWorldPosition(GridCoordinateFromArrayIndexFrom(i) + directions[flow]);
            target.y = data.Height[i];

            Vector3 dir = target - position;

            Debug.DrawRay(position, dir * 0.5f, c);

            Gizmos.color = color;
        }
    }
}