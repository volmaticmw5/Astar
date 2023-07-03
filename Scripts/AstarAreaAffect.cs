using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AstarNavigation
{
    [RequireComponent(typeof(BoxCollider))]
    [ExecuteInEditMode]
    public class AstarAreaAffect : MonoBehaviour
    {
        [Serializable]
        public enum AFFECT_TYPE
        {
            AABB_BLOCK,
            AABB_COST_SET,
            BLOCK,
            COST_SET,
        }
        
        public AstarMesh NavMesh;
        public AFFECT_TYPE Type;
        [Range(0.0f, 10f)]
        public float SizeMargin = 0.1f;

        [HideInInspector] public float SetWeight;
        
        private BoxCollider _area;
        private Vector3 _lastPos;
        private Vector3 _previousSizeMin, _previousSizeMax;
        private List<AstarNode> _pointsSetBlockedPreviously;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private async void Initialize()
        {
            _pointsSetBlockedPreviously = new List<AstarNode>();
            _area = GetComponent<BoxCollider>();
            _area.isTrigger = true;
            _previousSizeMax = _area.bounds.max;
            _previousSizeMin = _area.bounds.min;
            _lastPos = Vector3.zero;

            while (NavMesh == null)
                await Task.Delay(100);
            
            if(!NavMesh.AffectAreas.Contains(this))
                NavMesh.AffectAreas.Add(this);
        }

        private void OnDrawGizmosSelected()
        {
            if(_area == null)
                _area = GetComponent<BoxCollider>();
            
            Gizmos.color = Color.cyan;
            Handles.Label(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, _area.size.z)*0.5f), "P1");
            Gizmos.DrawSphere(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, _area.size.z)*0.5f), .05f);
            Handles.Label(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, -_area.size.z)*0.5f), "P2");
            Gizmos.DrawSphere(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, -_area.size.z)*0.5f), .05f);
            Handles.Label(_area.transform.TransformPoint(_area.center + new Vector3(_area.size.x, -_area.size.y, _area.size.z)*0.5f), "P4");
            Gizmos.DrawSphere(_area.transform.TransformPoint(_area.center + new Vector3(_area.size.x, -_area.size.y, _area.size.z)*0.5f), .05f);
            Handles.Label(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, _area.size.y, _area.size.z)*0.5f), "P5");
            Gizmos.DrawSphere(_area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, _area.size.y, _area.size.z)*0.5f), .05f);
        }

        public void Reset()
        {
            if (_pointsSetBlockedPreviously != null)
            {
                foreach (AstarNode node in _pointsSetBlockedPreviously)
                {
                    node.Passable = true;
                    node.Cost = AstarMesh.DEFAULT_NODE_COST;

                    _lastPos = Vector3.zero;
                }
            }
        }

        private void Update()
        {
            if (NavMesh == null)
                return;
            if (NavMesh.Nodes == null)
                return;
            if (NavMesh.Nodes.Count == 0)
                return;
            
            // Update navmesh if area moved
            if (_lastPos != transform.position || _previousSizeMax != _area.bounds.max || _previousSizeMin != _area.bounds.min)
            {
                switch (Type)
                {
                    case AFFECT_TYPE.BLOCK:
                        if (_pointsSetBlockedPreviously.Count > 0)
                        {
                            foreach (AstarNode node in _pointsSetBlockedPreviously)
                            {
                                node.Passable = true;
                            }
                        }
                        
                        _pointsSetBlockedPreviously = new List<AstarNode>(NavMesh.SetDynamicPassable(
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, _area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, -_area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(_area.size.x, -_area.size.y, _area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, _area.size.y, _area.size.z)*0.5f),
                            false
                        ));
                        
                        break;
                    case AFFECT_TYPE.COST_SET:
                        if (_pointsSetBlockedPreviously.Count > 0)
                        {
                            foreach (AstarNode node in _pointsSetBlockedPreviously)
                            {
                                node.Cost = AstarMesh.DEFAULT_NODE_COST;
                            }
                        }
                        
                        _pointsSetBlockedPreviously = new List<AstarNode>(NavMesh.SetDynamicWeight(
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, _area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, -_area.size.y, -_area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(_area.size.x, -_area.size.y, _area.size.z)*0.5f),
                            _area.transform.TransformPoint(_area.center + new Vector3(-_area.size.x, _area.size.y, _area.size.z)*0.5f),
                            SetWeight
                        ));
                        
                        break;
                    case AFFECT_TYPE.AABB_COST_SET:
                        if (_pointsSetBlockedPreviously.Count > 0)
                        {
                            foreach (AstarNode node in _pointsSetBlockedPreviously)
                            {
                                node.Cost = AstarMesh.DEFAULT_NODE_COST;
                            }
                        }
                        
                        _pointsSetBlockedPreviously = new List<AstarNode>(NavMesh.SetWeight(_area.bounds.min, _area.bounds.max, SizeMargin, SetWeight));
                        
                        break;
                    default:
                    case AFFECT_TYPE.AABB_BLOCK:
                        if (_pointsSetBlockedPreviously.Count > 0)
                        {
                            foreach (AstarNode node in _pointsSetBlockedPreviously)
                            {
                                node.Passable = true;
                            }
                        }
                        
                        _pointsSetBlockedPreviously = new List<AstarNode>(NavMesh.SetPassable(_previousSizeMin, _previousSizeMax, SizeMargin, false));
                        
                        break;
                }

                _previousSizeMin = _area.bounds.min;
                _previousSizeMax = _area.bounds.max;
                _lastPos = transform.position;
            }
        }
    }
}

