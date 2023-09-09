using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;

public class MoveHierarchyByChild : EditorWindow
{
    private static EditorWindow Window = null;
    private Transform fromObject = null;
    private Transform toObject = null;


    [MenuItem("GameObject/+Move Hierarchy By Child")]
    private static void Init()
    {
        Window = EditorWindow.GetWindow<MoveHierarchyByChild>();
        
        //Create the Editor Window
        Window.minSize = new Vector2(350, 260);
        Window.maxSize = new Vector2(350, 260);
        Window.titleContent = new GUIContent("Move Hierarchy By Child");

        Window.Show(); //Draw
    }

    private void OnGUI()
    {
        //Create 2 fields for the transforms you wish to move to the same world point (Carrying the parent)
        fromObject = (Transform)EditorGUILayout.ObjectField("Child To Move", fromObject, typeof(Transform), true);
        toObject = (Transform)EditorGUILayout.ObjectField("Destination Transform", toObject, typeof(Transform), true);

        if(fromObject != null && toObject != null)
        { 
            //Create button, extend it to the whole window horizontally, and perferm the snap
            if(GUILayout.Button("Perform Move Hierarchy", GUILayout.ExpandWidth(true)))
            {
                Vector3 targetPos = toObject.position;
                Vector3 parentPos = fromObject.root.position; 
                Vector3 amountToOffset = targetPos - fromObject.position;

                //Add the offset to the parent (Final positon => child is snapped exactly to the target)
                fromObject.root.position += amountToOffset;
            }
        }
    }
}
