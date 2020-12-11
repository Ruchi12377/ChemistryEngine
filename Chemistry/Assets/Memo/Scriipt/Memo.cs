using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "Memo")]
public class Memo : ScriptableObject
{
   public bool closed;
   public string title;
   [TextArea(50, 100)] 
   public string contents;
}

[CustomEditor(typeof(Memo))] //!< 拡張するときのお決まりとして書いてね
public class CharacterEditor : Editor //!< Editorを継承するよ！
{
   public override void OnInspectorGUI()
   {
      var memo = target as Memo;
      memo.closed = EditorGUILayout.ToggleLeft("Closed", memo.closed);
      if(memo.closed)EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.LabelField("Title");
      memo.title = EditorGUILayout.TextField(memo.title);
      EditorGUILayout.LabelField("Contents");
      memo.contents = EditorGUILayout.TextArea(memo.contents);
      
      if(memo.closed)EditorGUI.EndDisabledGroup();
   }
}