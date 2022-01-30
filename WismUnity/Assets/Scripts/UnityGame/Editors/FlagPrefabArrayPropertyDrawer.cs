using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Assets.Scripts.Editors
{
    // TODO: Reconcile with ArmyPrefabArrayLayout to avoid redundancy
    [CustomPropertyDrawer(typeof(FlagPrefabArrayLayout))]
    public class FlagPrefabArrayPropertyDrawer : PropertyDrawer
    {
        public const float DefaultEntryHeight = 18f;
        public const float ClanLabelWidth = 35f;

        private int clanCount;
        private int flagCount;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PrefixLabel(position, label);

            this.clanCount = property.FindPropertyRelative("count").intValue;
            Rect newPosition = position;
            newPosition.y += DefaultEntryHeight;
            SerializedProperty data = property.FindPropertyRelative("rows");

            for (int j = 0; j < this.clanCount; j++)
            {
                // Add clan label
                newPosition.height = DefaultEntryHeight;
                newPosition.width = ClanLabelWidth;
                GUIContent newLabel = new GUIContent("Clan:");
                EditorGUI.LabelField(newPosition, newLabel);

                // Get flag count for this clan
                this.flagCount = data.GetArrayElementAtIndex(j).FindPropertyRelative("count").intValue;

                // Add clan name entry box                                
                newPosition.width = position.width - ClanLabelWidth;
                newPosition.x = position.x + ClanLabelWidth;
                SerializedProperty name = data.GetArrayElementAtIndex(j).FindPropertyRelative("name");
                EditorGUI.PropertyField(newPosition, name, GUIContent.none);

                // Display flag game object entry boxes (one per flag kind)
                SerializedProperty row = data.GetArrayElementAtIndex(j).FindPropertyRelative("row");
                SerializedProperty rowNames = data.GetArrayElementAtIndex(j).FindPropertyRelative("rowNames");
                if (row.arraySize != this.flagCount)
                {
                    row.arraySize = this.flagCount;
                }
                newPosition.y += DefaultEntryHeight;
                for (int i = 0; i < this.flagCount; i++)
                {
                    // Add flag-kind name label
                    newLabel = new GUIContent(rowNames.GetArrayElementAtIndex(i).stringValue);
                    EditorGUI.LabelField(newPosition, newLabel);
                    newPosition.y += DefaultEntryHeight;

                    // Add GameObject entry box
                    EditorGUI.PropertyField(newPosition, row.GetArrayElementAtIndex(i), GUIContent.none);
                    newPosition.y += DefaultEntryHeight;
                }

                newPosition.x = position.x;
                newPosition.y += DefaultEntryHeight;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return
                (DefaultEntryHeight * (this.clanCount + 1) * 2) +
                ((DefaultEntryHeight * (this.flagCount * 2) * this.clanCount));
        }
    }
}

#endif