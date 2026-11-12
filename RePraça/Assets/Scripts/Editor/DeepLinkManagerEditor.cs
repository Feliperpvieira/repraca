using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeepLinkManager))]
public class DeepLinkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Teste de Universal Link", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Edite o campo 'Url De Teste' acima e clique no botão. O teste usa exactamente o mesmo fluxo dos links recebidos pela aplicação.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Testar link acima", GUILayout.Height(32)))
            {
                ((DeepLinkManager)target).SimularDeepLink();
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Entre em Play Mode para testar o link.", MessageType.None);
        }
    }
}
