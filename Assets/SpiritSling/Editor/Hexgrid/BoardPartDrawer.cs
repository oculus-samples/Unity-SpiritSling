// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace SpiritSling.TableTop.Editor
{
    [CustomPropertyDrawer(typeof(BoardPart))]
    public class BoardPartDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var x = position.x;
            var y = position.y;
            var w = position.width;
            var h = EditorGUIUtility.singleLineHeight;
            var currentRect = new Rect(x, y, w, h);

            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("partName"));
            currentRect.y += currentRect.height;
            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("weight"));
            currentRect.y += currentRect.height;
            EditorGUI.PropertyField(currentRect, property.FindPropertyRelative("rotationEnabled"));
            currentRect.y += currentRect.height;

            var elementsProperty = property.FindPropertyRelative("elements");

            // Ensure the array has the correct number of elements
            if (elementsProperty.arraySize != BoardPart.EXPECTED_SLICE_SIZE)
            {
                elementsProperty.arraySize = BoardPart.EXPECTED_SLICE_SIZE;

                // Create an hexa pizza slice for a radius of 5
                //     o
                //    o o 
                //   o o o
                //  o o o o
                // o o o o o 
                elementsProperty.GetArrayElementAtIndex(0).boxedValue = new TileElement(BoardPart.Positions[0]);
                elementsProperty.GetArrayElementAtIndex(1).boxedValue = new TileElement(BoardPart.Positions[1]);
                elementsProperty.GetArrayElementAtIndex(2).boxedValue = new TileElement(BoardPart.Positions[2]);
                elementsProperty.GetArrayElementAtIndex(3).boxedValue = new TileElement(BoardPart.Positions[3]);
                elementsProperty.GetArrayElementAtIndex(4).boxedValue = new TileElement(BoardPart.Positions[4]);
                elementsProperty.GetArrayElementAtIndex(5).boxedValue = new TileElement(BoardPart.Positions[5]);
                elementsProperty.GetArrayElementAtIndex(6).boxedValue = new TileElement(BoardPart.Positions[6]);
                elementsProperty.GetArrayElementAtIndex(7).boxedValue = new TileElement(BoardPart.Positions[7]);
                elementsProperty.GetArrayElementAtIndex(8).boxedValue = new TileElement(BoardPart.Positions[8]);
                elementsProperty.GetArrayElementAtIndex(9).boxedValue = new TileElement(BoardPart.Positions[9]);
                elementsProperty.GetArrayElementAtIndex(10).boxedValue = new TileElement(BoardPart.Positions[10]);
                elementsProperty.GetArrayElementAtIndex(11).boxedValue = new TileElement(BoardPart.Positions[11]);
                elementsProperty.GetArrayElementAtIndex(12).boxedValue = new TileElement(BoardPart.Positions[12]);
                elementsProperty.GetArrayElementAtIndex(13).boxedValue = new TileElement(BoardPart.Positions[13]);
                elementsProperty.GetArrayElementAtIndex(14).boxedValue = new TileElement(BoardPart.Positions[14]);
            }

            // Force positions in case of copies
            for (var i = 0; i < elementsProperty.arraySize; i++)
            {
                var t = (TileElement)elementsProperty.GetArrayElementAtIndex(i).boxedValue;
                if (t.q != BoardPart.Positions[i].x || t.r != BoardPart.Positions[i].y)
                {
                    t.q = BoardPart.Positions[i].x;
                    t.r = BoardPart.Positions[i].y;
                    elementsProperty.GetArrayElementAtIndex(i).boxedValue = t;
                }
            }

            currentRect.height = 4f * EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(currentRect, elementsProperty);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var elementsProperty = property.FindPropertyRelative("elements");

            var h = EditorGUIUtility.singleLineHeight * 4f;

            if (elementsProperty.isExpanded && elementsProperty.arraySize > 0)
            {
                // Calculate height: 2 properties + 1 line for the foldout, plus each element's height
                return h
                       + EditorGUIUtility.singleLineHeight
                       + elementsProperty.arraySize * (2f + EditorGUIUtility.singleLineHeight * 3)
                       + 2 * EditorGUIUtility.singleLineHeight; // Internal array + -
            }

            return h;
        }
    }

    [CustomPropertyDrawer(typeof(TileElement))]
    public class TileElementDrawer : PropertyDrawer
    {
        private GUIStyle textStyle;

        public TileElementDrawer()
        {
            textStyle = new GUIStyle(EditorStyles.label);
            textStyle.alignment = TextAnchor.MiddleRight;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Calculate the height needed for this property drawer
            return 2f + EditorGUIUtility.singleLineHeight * 3; // 2 fields + 1 header + padding
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Begin property drawing
            EditorGUI.BeginProperty(position, label, property);

            // Calculate field positions
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var padding = 2f;
            var fieldHeight = lineHeight + padding;

            var currentRect = new Rect(position.x, position.y, position.width, lineHeight);

            // Draw the label
            var q = property.FindPropertyRelative("q").intValue;
            var r = property.FindPropertyRelative("r").intValue;
            var hex = $"({q},{r})";
            EditorGUI.LabelField(currentRect, hex, EditorStyles.boldLabel);
            currentRect.y += fieldHeight;

            // Draw the fields
            const int elementsByLine = 3;
            var w = (int)(position.width / (elementsByLine * 2));
            currentRect = new Rect(currentRect.x, currentRect.y, w, lineHeight);
            EditorGUI.LabelField(currentRect, "Lava  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("heightLavaPercent"), new GUIContent(string.Empty));
            currentRect.x += w;
            EditorGUI.LabelField(currentRect, "0  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("height0Percent"), new GUIContent(string.Empty));
            currentRect.x += w;
            EditorGUI.LabelField(currentRect, "1  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("height1Percent"), new GUIContent(string.Empty));
            currentRect.x = position.x;
            currentRect.y += fieldHeight;
            EditorGUI.LabelField(currentRect, "2  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("height2Percent"), new GUIContent(string.Empty));
            currentRect.x += w;
            EditorGUI.LabelField(currentRect, "3  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("height3Percent"), new GUIContent(string.Empty));
            currentRect.x += w;
            EditorGUI.LabelField(currentRect, "4  ", textStyle);
            currentRect.x += w;
            EditorGUI.PropertyField(
                currentRect, property.FindPropertyRelative("height4Percent"), new GUIContent(string.Empty));
            currentRect.x += w;

            // EditorGUI.LabelField(currentRect, "5  ", textStyle);
            // currentRect.x += w;
            // EditorGUI.PropertyField(
            //     currentRect, property.FindPropertyRelative("height5Percent"), new GUIContent(string.Empty));

            // Restore indent
            EditorGUI.indentLevel = indent;

            // End property drawing
            EditorGUI.EndProperty();
        }
    }
}