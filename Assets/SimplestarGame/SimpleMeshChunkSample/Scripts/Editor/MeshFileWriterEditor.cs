using UnityEngine;
using UnityEditor;

namespace SimplestarGame
{
    [CustomEditor(typeof(MeshFileWriter))]
    public class MMeshFileWriterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = (MeshFileWriter)target;

            if (GUILayout.Button("Write"))
            {
                script.WriteMeshFile();
            }
        }
    }
}