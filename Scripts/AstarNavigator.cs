using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AstarNavigation
{
    [ExecuteInEditMode]
    public class AstarNavigator : MonoBehaviour
    {
        public float NextNodeDistanceRadius = 0.2f;
        public AstarMesh NavMesh;

        private Queue<AstarNode> pathQueue;
        private Action _pathCallback;
        private Vector3 _currentTargetPosition = Vector3.negativeInfinity;
        private bool _reachedCurrentTargetPosition = true;
        private bool _traveling = false;
        
        private Dictionary<AstarNode, float> _debugPath;
        private Dictionary<AstarNode, float> _debugNeighbourCost;

        private void Awake()
        {
            _currentTargetPosition = Vector3.negativeInfinity;
            _reachedCurrentTargetPosition = true;
        }

        public void GoTo(Vector3 position, Action callback = null)
        {
            AstarNode[] path = GetPathToPosition(position);
            pathQueue = new Queue<AstarNode>(path);
            _reachedCurrentTargetPosition = true;
            _pathCallback = callback;
            _traveling = true;
        }

        private void Update()
        {
            if (_traveling)
            {
                if (_reachedCurrentTargetPosition)
                {
                    if (pathQueue.Count == 0)
                    {
                        _currentTargetPosition = Vector3.negativeInfinity;
                        _reachedCurrentTargetPosition = true;
                        _traveling = false;
                    
                        if (_pathCallback != null)
                            _pathCallback();
                    }
                    else
                    {
                        _currentTargetPosition = pathQueue.Dequeue().GlobalPosition;
                        _reachedCurrentTargetPosition = false;
                    }
                }
            
                if (_currentTargetPosition != Vector3.negativeInfinity && !_reachedCurrentTargetPosition)
                {
                    transform.position = Vector3.MoveTowards(transform.position, _currentTargetPosition, Time.deltaTime * 2f);
            
                    if (Vector3.Distance(transform.position, _currentTargetPosition) <= NextNodeDistanceRadius)
                    {
                        _reachedCurrentTargetPosition = true;
                    }
                }
            }
        }

        private AstarNode[] GetPathToPosition(Vector3 position)
        {
    #if UNITY_EDITOR
            _debugPath = new Dictionary<AstarNode, float>();
            _debugNeighbourCost = new Dictionary<AstarNode, float>();
    #endif
            
            bool reached = false;
            AstarNode target = NavMesh.GetClosestNodeFromPosition(position);
            AstarNode current = NavMesh.GetClosestNodeFromPosition(transform.position);
            
            List<AstarNode> path = new List<AstarNode>();
            int distanceToTarget = (int)Vector3.Distance(current.GlobalPosition, target.GlobalPosition);

            if (distanceToTarget <= 1)
                return new AstarNode[] { target };
            
            while (!reached)
            {
                AstarNode[] neighbours = NavMesh.GetNeighbours(current);
                Dictionary<AstarNode, float> neighbourWeights = new Dictionary<AstarNode, float>();
                foreach (AstarNode neighbour in neighbours)
                {
                    if (neighbour == current)
                        continue;

                    if (path.Contains(neighbour))
                        continue;
                    
                    if (neighbour == null)
                        continue;

                    if (!neighbour.Passable)
                        continue;

                    neighbourWeights.Add(neighbour, (Vector3.Distance(neighbour.GlobalPosition, target.GlobalPosition) * NavMesh.DistanceWeight) + neighbour.Cost);

    #if UNITY_EDITOR
                    if(!_debugNeighbourCost.ContainsKey(neighbour))
                        _debugNeighbourCost.Add(neighbour, (Vector3.Distance(neighbour.GlobalPosition, target.GlobalPosition) * NavMesh.DistanceWeight) + neighbour.Cost);
    #endif
                }

                if (current == target)
                {
                    reached = true;
                    break;
                }

                AstarNode fittestNeighbour = neighbourWeights.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                
                if (path.Contains(fittestNeighbour))
                {
                    reached = true;
                    break;
                }

                path.Add(fittestNeighbour);
                
    #if UNITY_EDITOR
                _debugPath.Add(fittestNeighbour, (Vector3.Distance(fittestNeighbour.GlobalPosition, target.GlobalPosition) * NavMesh.DistanceWeight) + fittestNeighbour.Cost);
    #endif
                
                current = fittestNeighbour;
            }

            return path.ToArray();
        }

        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            if (!NavMesh.ShowingDebug)
                return;
            if (_debugPath == null)
                return;
            if (_debugPath.Count == 0)
                return;

            AstarNode previous = null;
            foreach (KeyValuePair<AstarNode, float> node in _debugPath)
            {
                Gizmos.DrawLine(previous != null ? previous.GlobalPosition : transform.position,  node.Key.GlobalPosition);
                previous = node.Key;
            }

            foreach (KeyValuePair<AstarNode,float> node in _debugNeighbourCost)
            {
                Handles.Label(node.Key.GlobalPosition + new Vector3(0,0.8f,0), node.Value.ToString(), new GUIStyle()
                {
                    normal = new GUIStyleState()
                    {
                        textColor = _debugPath.ContainsKey(node.Key) ? Color.cyan : Color.yellow
                    }
                });
            }
            
            // Force Update() and editor repaint in edit mode
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
    #endif
        }
    }
}
