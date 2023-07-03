using System;
using UnityEngine;

namespace AstarNavigation
{
    [Serializable]
    public class AstarNode
    {
        public Vector3 GlobalPosition = Vector3.zero;
        public float Cost = AstarMesh.DEFAULT_NODE_COST;
        public bool Passable;
    
        public AstarNode(Vector3 pos, float cost = 100f, bool passable = true)
        {
            GlobalPosition = pos;
            Cost = cost;
            Passable = passable;
        }
    }
}
