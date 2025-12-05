using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(Image_TwoTexMul))]
public class Image_TwoTexMulEditor : ImageEditor
{
    SerializedProperty mainTexUV;
    SerializedProperty alphaTexUV;

    protected override void OnEnable()
    {
        base.OnEnable();

        mainTexUV = serializedObject.FindProperty("_mainTexUV");
        alphaTexUV = serializedObject.FindProperty("_alphaTexUV");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UV Settings (TwoTexture)", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(mainTexUV, new GUIContent("Texture1 UV (ScaleX,ScaleY,OffsetX,OffsetY)"));
        EditorGUILayout.PropertyField(alphaTexUV, new GUIContent("AlphaMap UV (ScaleX,ScaleY,OffsetX,OffsetY)"));

        serializedObject.ApplyModifiedProperties();
    }
}
