using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Utilities.ScriptableFactory
{
    public class ScriptableFactory<T> : PropertyDrawer where T : ScriptableObject
    {
        float divider = 0.8f;

        List<string> componentNames;
        int selected;

        int tempIndent;

        /// <summary>
        /// Gets the height of the property.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.objectReferenceValue == null)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                return height;
            }
            else if (!property.isExpanded)
            {
                return height;
            }

            SerializedObject so_object = new SerializedObject(property.objectReferenceValue);
            SerializedProperty field = so_object.GetIterator();
            field.Next(true);

            while (field.NextVisible(false))
            {
                if (field.type == "PPtr<MonoScript>")
                {
                    continue;
                }

                height += EditorGUI.GetPropertyHeight(field, new GUIContent(field.name)) + EditorGUIUtility.standardVerticalSpacing;

                //// Display arrays
                //if (field.isArray)
                //{
                //    SerializedProperty c_Field;

                //    for (int i = 0; i < field.arraySize; i++)
                //    {
                //        c_Field = field.GetArrayElementAtIndex(i);
                //        height += EditorGUI.GetPropertyHeight(c_Field, new GUIContent(c_Field.name)) + EditorGUIUtility.standardVerticalSpacing;
                //    }
                //}
            }

            return height;
        }

        /// <summary>
        /// OnGUI.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Object is null
            if (property.objectReferenceValue == null)
            {
                // Property field
                EditorGUI.PropertyField(
                    new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                    property,
                    false);

                InitFactory();

                // Create instance <T> of type selected
                tempIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                CreateInstance(position, property);
                EditorGUI.indentLevel = tempIndent;

                return;
            }

            // Object is NOT null
            // Expand property
            property.isExpanded = EditorGUI.Foldout(
                new Rect(
                    position.x,
                    position.y,
                    EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                property.displayName);

            // Property field
            tempIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(
                new Rect(
                    position.x + EditorGUIUtility.labelWidth,
                    position.y,
                    position.width * divider - 2 - EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight),
                property,
                GUIContent.none,
                false);

            // Delete referenced object
            if (DeleteInstance(position, property))
            {
                return;
            }
            EditorGUI.indentLevel = tempIndent;

            // Display properties
            if (!property.isExpanded) { return; }

            EditorGUI.indentLevel += 1;
            DisplayProperties(position, property);
            EditorGUI.indentLevel -= 1;

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Adds the types' names into the list
        /// </summary>
        private void InitFactory()
        {
            // Get all <T> types
            var componentTypes =
                Assembly.GetAssembly(typeof(T)).GetTypes().
                Where(t =>
                      t.IsSubclassOf(typeof(T)) &&
                      !t.IsAbstract);

            // Add them to list
            componentNames = new List<string>();
            foreach (var type in componentTypes)
            {
                componentNames.Add(type.ToString());
            }
        }

        /// <summary>
        /// Creates an instance of the selected type.
        /// </summary>
        private void CreateInstance(Rect position, SerializedProperty property)
        {
            Rect fieldRect =
                new Rect(
                    position.x + EditorGUIUtility.labelWidth,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width - EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight);

            // Select type from the Popup
            selected = EditorGUI.Popup(
                new Rect(
                    fieldRect.x,
                    fieldRect.y,
                    fieldRect.width * divider - 2,
                    fieldRect.height),
                selected,
                componentNames.ToArray());

            // Button to confirm selection and create the instance
            if (GUI.Button(
                new Rect(
                    fieldRect.x + fieldRect.width * divider,
                    fieldRect.y,
                    fieldRect.width * (1 - divider),
                    fieldRect.height),
                new GUIContent("Create")))
            {
                T component = ScriptableObject.CreateInstance(componentNames[selected]) as T;
                component.name = componentNames[selected];

                //if (EditorUtility.IsPersistent(property.serializedObject.targetObject))
                //{
                //    AssetDatabase.AddObjectToAsset(component, property.serializedObject.targetObject);
                //    AssetDatabase.SaveAssets();
                //}
                //else
                //{
                string savePath = EditorUtility.SaveFilePanelInProject(
                    "Save ScriptableObject", 
                    component.name + ".asset", 
                    "asset",
                    "Save the ScriptableObject as an asset.",
                    AssetDatabase.GetAssetPath(Selection.activeObject) + "Assets"
                );
                //}


                if (!string.IsNullOrEmpty(savePath))
                {
                    Debug.Log(savePath);
                    AssetDatabase.CreateAsset(component, savePath);
                    AssetDatabase.SaveAssets();

                    property.objectReferenceValue = component;
                }
            }
        }

        /// <summary>
        /// Destroys the instance.
        /// </summary>
        private bool DeleteInstance(Rect position, SerializedProperty property)
        {
            if (GUI.Button(
                    new Rect(
                        position.x + position.width * divider,
                        position.y,
                        position.width * (1 - divider),
                        EditorGUIUtility.singleLineHeight),
                    new GUIContent("Delete")))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(property.objectReferenceValue));
                AssetDatabase.SaveAssets();

                property.objectReferenceValue = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Displaies the instance's properties.
        /// </summary>
        void DisplayProperties(Rect position, SerializedProperty property)
        {
            // Get all serializable properties in the object
            SerializedObject so_object = new SerializedObject(property.objectReferenceValue);
            SerializedProperty field = so_object.GetIterator();
            field.Next(true);

            float y = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            so_object.Update();

            while (field.NextVisible(false))
            {
                // Skip MonoScript field
                if (field.type == "PPtr<MonoScript>")
                {
                    continue;
                }

                // Property field
                EditorGUI.PropertyField(
                    new Rect(position.x, position.y + y, position.width, EditorGUIUtility.singleLineHeight),
                    field,
                    false);

                // Display array
                if (field.isArray &&
                    field.isExpanded)
                {
                    EditorGUI.indentLevel += 1;
                    y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    SerializedProperty c_Field = null;
                    float c_Heigth = 0;

                    // Size
                    EditorGUI.PropertyField(
                        new Rect(
                            position.x,
                            position.y + y,
                            position.width,
                            EditorGUIUtility.singleLineHeight),
                        field.FindPropertyRelative("Array.size"));
                    y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    // Array elements
                    for (int i = 0; i < field.arraySize; i++)
                    {
                        c_Field = field.GetArrayElementAtIndex(i);
                        c_Heigth = EditorGUI.GetPropertyHeight(c_Field, new GUIContent(c_Field.name)) + EditorGUIUtility.standardVerticalSpacing;

                        EditorGUI.PropertyField(
                            new Rect(
                                position.x,
                                position.y + y,
                                position.width,
                                c_Heigth),
                            c_Field,
                            false);

                        y += c_Heigth;
                    }
                    EditorGUI.indentLevel -= 1;
                }
                // Don't display array
                else
                {
                    y += EditorGUI.GetPropertyHeight(field, new GUIContent(field.name)) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            so_object.ApplyModifiedProperties();
        }
    }
}