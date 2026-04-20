using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FixedJoystick))]
public class FixedJoystickEditor : JoystickEditor
{
    private SerializedProperty hideBackground;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        hideBackground = serializedObject.FindProperty("hideBackground");
    }
    
    protected override void DrawValues()
    {
        base.DrawValues();
        EditorGUILayout.PropertyField(hideBackground, new GUIContent("Hide Background", "Hides the background image when the joystick is not being used."));
    }
}