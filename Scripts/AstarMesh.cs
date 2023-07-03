using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace AstarNavigation
{
    [ExecuteInEditMode]
    public class AstarMesh : MonoBehaviour
    {
        public static readonly float DEFAULT_NODE_COST = 100f;
        [HideInInspector] public List<AstarAreaAffect> AffectAreas;

        public float DistanceWeight = 500f;
        [HideInInspector] public int ID;
        [HideInInspector] public bool IsTerrain = false;
        [HideInInspector] public TerrainData TerrainData;
        [HideInInspector] public float MinHeight;
        [HideInInspector] public float MaxHeight;
        
        [HideInInspector] public MeshFilter MeshFilter;
        
        [HideInInspector] public bool ShowingDebug;
        [HideInInspector] public float GizmosDrawDistance = 25f;
        [HideInInspector] public bool GizmosDrawVertice;

        private Dictionary<Vector3, AstarNode> _debugDrawNodes;
        private Thread _updateDebugDrawNodesThread;
        private Vector3 _sceneCameraPos;
        private SceneView _sceneView;
        private string _navMeshName;

        [HideInInspector] public AstarNavigator _debugNavigator;
        [HideInInspector] public GameObject _debugDestinationObj;

        [HideInInspector] public Dictionary<Vector3, AstarNode> Nodes { get; private set; }

        public void LoadNodes()
        {
            ClearNodes();

            if (!File.Exists($"{Application.dataPath + "/game_data/" + ID + "_nodes.nav"}"))
            {
                Debug.Log($"Couldn't load node data, there's no such file: {Application.dataPath + "/game_data/" + ID + "_nodes.nav"}");
                return;
            }

            using (FileStream fs = new FileStream($"{Application.dataPath + "/game_data/" + _navMeshName}", FileMode.Open))
            {
                BinaryReader reader = new(fs);
                int nodes = reader.ReadInt32();
                
                for (int i = 0; i < nodes; i++)
                {
                    Vector3 pos = new Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
                    Nodes.Add(pos, new AstarNode(pos, (float)reader.ReadDouble()));
                }

                Debug.Log($"Read {nodes} nodes successfully.");
                fs.Close();
            }
        }

        public AstarNode GetNodeAtPosition(Vector3 pos)
        {
            if (Nodes.ContainsKey(new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z))))
            {
                return Nodes[pos];
            }

            return null;
        }

        public AstarNode GetClosestNodeFromPosition(Vector3 pos)
        {
            float closestDistance = 9999999f;
            AstarNode closest = null;
            foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
            {
                float dist = Vector3.Distance(node.Value.GlobalPosition, pos);
                if (dist < closestDistance)
                {
                    closest = node.Value;
                    closestDistance = dist;
                }
            }

            return closest;
        }

        public AstarNode[] GetNeighbours(AstarNode node)
        {
            AstarNode[] result = new AstarNode[8];
            result[0] = GetNodeAtPosition(node.GlobalPosition - new Vector3(-1, 0, 1));
            result[1] = GetNodeAtPosition(node.GlobalPosition - new Vector3(0, 0, 1));
            result[2] = GetNodeAtPosition(node.GlobalPosition - new Vector3(1, 0, 1));
            result[3] = GetNodeAtPosition(node.GlobalPosition - new Vector3(-1, 0, 0));
            result[4] = GetNodeAtPosition(node.GlobalPosition - new Vector3(1, 0, 0));
            result[5] = GetNodeAtPosition(node.GlobalPosition - new Vector3(-1, 0, -1));
            result[6] = GetNodeAtPosition(node.GlobalPosition - new Vector3(0, 0, -1));
            result[7] = GetNodeAtPosition(node.GlobalPosition - new Vector3(1, 0, -1));

            return result;
        }

        public AstarNode[] SetWeight(Vector3 from, Vector3 to, float range = 0.8f, float weight = 100f)
        {
            if (Nodes == null)
                return new AstarNode[]{};
            if (Nodes.Count == 0)
                return new AstarNode[]{};

            List<AstarNode> nodesUpdated = new List<AstarNode>();

            foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
            {
                if (node.Value.GlobalPosition.x >= from.x - range && node.Value.GlobalPosition.x <= to.x + range &&
                    node.Value.GlobalPosition.z >= from.z - range && node.Value.GlobalPosition.z <= to.z + range)
                {
                    node.Value.Cost = weight;
                    nodesUpdated.Add(node.Value);
                }
            }

            return nodesUpdated.ToArray();
        }
        
        public AstarNode[] SetPassable(Vector3 from, Vector3 to, float range = 0.8f, bool passable = true)
        {
            if (Nodes == null)
                return new AstarNode[]{};
            if (Nodes.Count == 0)
                return new AstarNode[]{};

            List<AstarNode> nodesUpdated = new List<AstarNode>();

            foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
            {
                if (node.Value.GlobalPosition.x >= from.x - range && node.Value.GlobalPosition.x <= to.x + range &&
                    node.Value.GlobalPosition.z >= from.z - range && node.Value.GlobalPosition.z <= to.z + range)
                {
                    node.Value.Passable = passable;
                    nodesUpdated.Add(node.Value);
                }
            }

            return nodesUpdated.ToArray();
        }

        public AstarNode[] SetDynamicPassable(Vector3 p1, Vector3 p2, Vector3 p4, Vector3 p5, bool passable)
        {
            List<AstarNode> nodesUpdated = new List<AstarNode>();
            foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
            {
                Vector3 i = p2 - p1;
                Vector3 j = p4 - p1;
                Vector3 k = p5 - p1;
                Vector3 v = node.Value.GlobalPosition - p1;

                var dotVI = Vector3.Dot(v, i);
                var dotVJ = Vector3.Dot(v, j);
                var dotVK = Vector3.Dot(v, k);
                var dotII = Vector3.Dot(i, i);
                var dotJJ = Vector3.Dot(j, j);
                var dotKK = Vector3.Dot(k, k);

                if (dotVI < dotII && dotVI > 0 && dotVJ > 0 && dotVK > 0 && dotVJ < dotJJ && dotVK < dotKK)
                {
                    node.Value.Passable = passable;
                    nodesUpdated.Add(node.Value);
                }
            }

            return nodesUpdated.ToArray();
        }

        public AstarNode[] SetDynamicWeight(Vector3 p1, Vector3 p2, Vector3 p4, Vector3 p5, float weight)
        {
            List<AstarNode> nodesUpdated = new List<AstarNode>();
            foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
            {
                Vector3 i = p2 - p1;
                Vector3 j = p4 - p1;
                Vector3 k = p5 - p1;
                Vector3 v = node.Value.GlobalPosition - p1;

                var dotVI = Vector3.Dot(v, i);
                var dotVJ = Vector3.Dot(v, j);
                var dotVK = Vector3.Dot(v, k);
                var dotII = Vector3.Dot(i, i);
                var dotJJ = Vector3.Dot(j, j);
                var dotKK = Vector3.Dot(k, k);

                if (dotVI < dotII && dotVI > 0 && dotVJ > 0 && dotVK > 0 && dotVJ < dotJJ && dotVK < dotKK)
                {
                    node.Value.Cost = weight;
                    nodesUpdated.Add(node.Value);
                }
            }

            return nodesUpdated.ToArray();
        }
        
        public void GenerateNodes()
        {
            ClearNodes();

            if (IsTerrain)
            {
                for (int z = 0; z < TerrainData.size.z; z++)
                {
                    for (int x = 0; x < TerrainData.size.x; x++)
                    {
                        float height = Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z));
                        
                        if (height >= MinHeight && height <= MaxHeight)
                        {
                            Nodes.Add(new Vector3(x, height, z), new AstarNode(new Vector3(x, height, z)));
                        }
                    }
                }
            }
            else
            {
                foreach (Vector3 vertex in MeshFilter.mesh.vertices)
                {
                    Nodes.Add(vertex, new AstarNode(vertex, 1f));
                } 
            }
            
            Debug.Log($"Generated {Nodes.Count} a* nodes for {gameObject.name}");

            _navMeshName = ID + "_nodes.nav";
            Thread ts = new Thread(SaveMeshData);
            ts.Start();
        }

        public void SaveMeshData()
        {
            if(!Directory.Exists($"{Application.dataPath}/game_data"))
                Directory.CreateDirectory($"{Application.dataPath}/game_data");

            using (FileStream fs = new FileStream($"{Application.dataPath + "/game_data/" + _navMeshName}", FileMode.Create))
            {
                using (BinaryWriter bw = new(fs))
                {
                    bw.Write(Nodes.Count);

                    foreach (KeyValuePair<Vector3,AstarNode> node in Nodes)
                    {
                        bw.Write((double)node.Value.GlobalPosition.x);
                        bw.Write((double)node.Value.GlobalPosition.y);
                        bw.Write((double)node.Value.GlobalPosition.z);
                        bw.Write((double)node.Value.Cost);
                    }
                }
                
                fs.Close();
            }
            
            Debug.Log($"Saved nav mesh data to {Application.dataPath + "/game_data/" + _navMeshName}");
        }
        
        public void ClearNodes()
        {
            DisableDebug();
            
            if (Nodes != null)
                Nodes.Clear();
            else
                Nodes = new Dictionary<Vector3, AstarNode>();
        }
        
        private void Awake()
        {
            AffectAreas = new List<AstarAreaAffect>();
            Nodes = new Dictionary<Vector3, AstarNode>();
        }
        
        private void Update()
        {
    #if UNITY_EDITOR
            if (ShowingDebug)
            {
                if(_sceneView == null)
                    _sceneView = SceneView.currentDrawingSceneView == null ? SceneView.lastActiveSceneView : SceneView.currentDrawingSceneView;
                
                _sceneCameraPos = _sceneView.camera.transform.position;
            }
    #endif
        }
        
        #region DEBUG
        private void UpdateDebugDrawNodes()
        {
            if (Nodes.Count == 0)
                return;
            
            while (ShowingDebug)
            {
                lock (_debugDrawNodes)
                {
                    _debugDrawNodes.Clear();
                
                    foreach (KeyValuePair<Vector3, AstarNode> node in Nodes)
                    {
                        if (Vector3.Distance(_sceneCameraPos, node.Value.GlobalPosition) <= GizmosDrawDistance)
                        {
                            _debugDrawNodes.Add(node.Value.GlobalPosition, node.Value);
                        }
                    }
                }

                Thread.Sleep(500);
            }
        }
        
        private void DebugDrawNodes()
        {
            if (_debugDrawNodes == null)
                return;

            Dictionary<Vector3, AstarNode> copy;
            lock (_debugDrawNodes)
            {
                copy = new Dictionary<Vector3, AstarNode>(_debugDrawNodes);
            }
            
            if (copy.Count == 0)
                return;
            
            foreach (KeyValuePair<Vector3, AstarNode> node in copy)
            {
                if (GizmosDrawVertice)
                {
                    if (node.Value.Passable)
                    {
                        if(node.Value.Cost <= 50f)
                            Gizmos.color = Color.blue;
                        else
                            Gizmos.color = Color.green;
                    }
                    
                    else
                        Gizmos.color = Color.gray;
                    
                    Gizmos.DrawSphere(node.Value.GlobalPosition, 0.1f);
                }
            
                Handles.Label(node.Value.GlobalPosition + new Vector3(0,0.2f,0), node.Value.Cost.ToString());
            }
        }

        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            if (ShowingDebug)
            {
                DebugDrawNodes();
                
                // Force Update() and editor repaint in edit mode
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
    #endif
        }

        public void EnableDebug()
        {
    #if UNITY_EDITOR
            _debugDrawNodes = new Dictionary<Vector3, AstarNode>();
            ShowingDebug = true;

            if (_updateDebugDrawNodesThread != null)
                _updateDebugDrawNodesThread.Join();
            
            _updateDebugDrawNodesThread = new Thread(UpdateDebugDrawNodes);
            _updateDebugDrawNodesThread.Start();
            
            _sceneView = SceneView.currentDrawingSceneView == null ? SceneView.lastActiveSceneView : SceneView.currentDrawingSceneView;
            _sceneCameraPos = _sceneView.camera.transform.position;
    #endif
        }
        
        public void DisableDebug()
        {
    #if UNITY_EDITOR
            ShowingDebug = false;
            
            if(_debugDrawNodes != null)
                _debugDrawNodes.Clear();

            if (_updateDebugDrawNodesThread != null)
            {
                _updateDebugDrawNodesThread.Join();
                _updateDebugDrawNodesThread = null;
            }
    #endif
        }

        public void DebugDestination()
        {
    #if UNITY_EDITOR
            _debugNavigator.GoTo(_debugDestinationObj.transform.position, () =>
            {
                Debug.Log("Debug go to destination finished!");
            });
    #endif
        }
        #endregion
    }
}
