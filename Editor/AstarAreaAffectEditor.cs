using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace AstarNavigation
{
    [CustomEditor(typeof(AstarAreaAffect))]
    public class AstarAreaAffectEditor : Editor
    {
        private AstarAreaAffect.AFFECT_TYPE _lastType;
        public AstarAreaAffect.AFFECT_TYPE _options;
    
        public override void OnInspectorGUI()
        {
            AstarAreaAffect obj = target as AstarAreaAffect;

            obj.NavMesh = EditorGUILayout.ObjectField("Nav Mesh", obj.NavMesh, typeof(AstarMesh), true) as AstarMesh;
            obj.Type = (AstarAreaAffect.AFFECT_TYPE)EditorGUILayout.EnumPopup("Type:", obj.Type);

            switch (obj.Type)
            {
                case AstarAreaAffect.AFFECT_TYPE.AABB_BLOCK:
                    EditorGUILayout.LabelField("Set a* nodes inside AABB area of box collider as impassable, with a margin.");
                    break;
                case AstarAreaAffect.AFFECT_TYPE.AABB_COST_SET:
                    EditorGUILayout.LabelField("Set a* nodes inside AABB area of box collider to defined cost/weight, with a margin.");
                    break;
                case AstarAreaAffect.AFFECT_TYPE.BLOCK:
                    EditorGUILayout.LabelField("Set a* nodes inside the area of box collider to impassable.");
                    break;
                case AstarAreaAffect.AFFECT_TYPE.COST_SET:
                    EditorGUILayout.LabelField("Set a* nodes inside the area of box collider to defined cost/weight.");
                    break;
            }
        
            if (obj.Type == AstarAreaAffect.AFFECT_TYPE.AABB_COST_SET || obj.Type == AstarAreaAffect.AFFECT_TYPE.COST_SET)
            {
                obj.SetWeight = EditorGUILayout.Slider("Node Cost/Weight", obj.SetWeight, 0f, 100f);
            }
            if(obj.Type == AstarAreaAffect.AFFECT_TYPE.AABB_BLOCK || obj.Type == AstarAreaAffect.AFFECT_TYPE.AABB_COST_SET)
            {
                obj.SizeMargin = EditorGUILayout.Slider("Margin", obj.SizeMargin, 0f, 10f);
            }

            if (_lastType != obj.Type)
                obj.Reset();

        
            _lastType = obj.Type;
        }
    }
}