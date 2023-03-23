using UnityEditor;

namespace ChemistryEngine.Script.NewCore
{
	[CustomEditor(typeof(Element))]
	public class ElementEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var chemistryObject = target as Element;
			if (chemistryObject.combustible && chemistryObject.liquid)
			{
				EditorGUILayout.HelpBox("燃える液体はサポートされていません", MessageType.Error);
			}
			base.OnInspectorGUI();
		}
	}

	[CustomEditor(typeof(Material))]
	public class MaterialEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var chemistryObject = target as Material;
			if (chemistryObject.combustible && chemistryObject.liquid)
			{
				EditorGUILayout.HelpBox("燃える液体はサポートされていません", MessageType.Error);
			}
			base.OnInspectorGUI();
		}
	}
}