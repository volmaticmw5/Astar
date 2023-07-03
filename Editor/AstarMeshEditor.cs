using UnityEngine;
using UnityEditor;

namespace AstarNavigation
{
    [CustomEditor(typeof(AstarMesh))]
    public class AstarMeshEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label("A* Mesh Actions", new GUIStyle
            {
                imagePosition = ImagePosition.ImageLeft,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            });
            GUILayout.Space(8);
            AstarMesh obj = target as AstarMesh;

            if (GUILayout.Button("Generate Navigation Nodes"))
            {
                obj.GenerateNodes();
            }
            
            GUILayout.Space(4);

            if (GUILayout.Button("Clear all nodes"))
            {
                obj.ClearNodes();
            }
            
            GUILayout.Space(4);

            if (GUILayout.Button("Try load navigation nodes"))
            {
                obj.LoadNodes();
            }
            
            GUILayout.Space(8);
            GUILayout.Label("A* Mesh Settings", new GUIStyle
            {
                imagePosition = ImagePosition.ImageLeft,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            });
            GUILayout.Space(8);

            obj.ID = EditorGUILayout.IntField("Unique ID", obj.ID);
            obj.DistanceWeight = EditorGUILayout.Slider("Distance Weight", obj.DistanceWeight, 1f, 500f);
            obj.IsTerrain = EditorGUILayout.Toggle("Is Terrain?", obj.IsTerrain);
            if (obj.IsTerrain)
            {
                obj.TerrainData = EditorGUILayout.ObjectField("Terrain Data", obj.TerrainData, typeof(TerrainData), false) as TerrainData;
                obj.MinHeight = EditorGUILayout.Slider("Min navigation height", obj.MinHeight, -1000f, 1000f);
                obj.MaxHeight = EditorGUILayout.Slider("Max navigation height", obj.MaxHeight, -1000f, 1000f);
            }
            else
            {
                obj.MeshFilter = EditorGUILayout.ObjectField("Mesh Filter", obj.MeshFilter, typeof(MeshFilter), false) as MeshFilter;
            }

            GUILayout.Space(8);
            GUILayout.Label("A* Debug Settings", new GUIStyle
            {
                imagePosition = ImagePosition.ImageLeft,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState()
                {
                    textColor = Color.white
                }
            });
            GUILayout.Space(8);
            if (obj.ShowingDebug)
            {
                if (GUILayout.Button("Hide debug gizmos"))
                {
                    obj.DisableDebug();
                }

                obj.GizmosDrawDistance = EditorGUILayout.Slider("Draw distance", obj.GizmosDrawDistance, 1f, 100f);
                obj.GizmosDrawVertice = EditorGUILayout.Toggle("Draw vertices", obj.GizmosDrawVertice);
            }
            else
            {
                if (GUILayout.Button("Show debug gizmos"))
                {
                    obj.EnableDebug();
                }
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Simulate entity travel"))
            {
                obj.DebugDestination();
            }

            obj._debugNavigator = EditorGUILayout.ObjectField("Navigator", obj._debugNavigator, typeof(AstarNavigator), true) as AstarNavigator;
            obj._debugDestinationObj = EditorGUILayout.ObjectField("Destination Object", obj._debugDestinationObj, typeof(GameObject), true) as GameObject;
        }
    }
}
