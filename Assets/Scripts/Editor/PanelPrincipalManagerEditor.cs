#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PanelPrincipalManager))]
public class PanelPrincipalManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Pruebas rapidas", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Ir rapido al Nivel 2"))
            {
                PanelPrincipalManager manager = (PanelPrincipalManager)target;
                manager.IrRapidoNivel2ParaPrueba();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("El boton se activa cuando presionas Play.", MessageType.Info);
        }
    }
}
#endif
