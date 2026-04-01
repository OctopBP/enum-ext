//HintName: WrapperForStateDrawer.g.cs
#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(WrapperForState))]
    public class WrapperForStateDrawer : PropertyDrawer
    {
        private const float RowHeight = 24f;
        private const float HeaderHeight = 20f;
        private const float LabelWidthRatio = 0.35f;
        private const float BorderWidth = 1f;
        private const float HorizontalPadding = 6f;
        private const float VerticalPadding = 4f;

        private static readonly string[] FieldNames =
        {
            "Idle",
            "Running",
        };

        private static readonly GUIStyle _headerStyle = new GUIStyle();
        private static readonly GUIStyle _columnHeaderStyle = new GUIStyle();
        private static readonly GUIStyle _cellStyle = new GUIStyle();
        private static Color _tableBorderColor;
        private static Color _borderColor;
        private static Color _stripeColor;
        private static bool _stylesInitialized;

        private static void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle.normal.textColor = EditorStyles.label.normal.textColor;
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            _headerStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);

            _columnHeaderStyle.normal.textColor = EditorStyles.label.normal.textColor;
            _columnHeaderStyle.alignment = TextAnchor.MiddleCenter;
            _columnHeaderStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);

            _cellStyle.normal.textColor = EditorStyles.label.normal.textColor;
            _cellStyle.alignment = TextAnchor.MiddleLeft;
            _cellStyle.padding = new RectOffset(left: 5, right: 5, top: 2, bottom: 2);

            _tableBorderColor = EditorGUIUtility.isProSkin
                ? new Color(r: 0.1f, g: 0.1f, b: 0.1f, a: 1f)
                : new Color(r: 0.4f, g: 0.4f, b: 0.4f, a: 1f);

            _borderColor = EditorGUIUtility.isProSkin
                ? new Color(r: 0.15f, g: 0.15f, b: 0.15f, a: 1f)
                : new Color(r: 0.5f, g: 0.5f, b: 0.5f, a: 1f);

            _stripeColor = EditorGUIUtility.isProSkin
                ? new Color(r: 1f, g: 1f, b: 1f, a: 0.05f)
                : new Color(r: 0f, g: 0f, b: 0f, a: 0.05f);

            _stylesInitialized = true;
        }

        private static float EstimateDrawerContentWidth()
        {
            return Mathf.Max(280f, EditorGUIUtility.currentViewWidth - 56f);
        }

        private static float GetValueColumnContentWidth(float tableOuterWidth)
        {
            var innerContentWidth = tableOuterWidth - BorderWidth * 2f;
            var labelColumnWidth = innerContentWidth * LabelWidthRatio;
            var valueColumnWidth = innerContentWidth - labelColumnWidth;
            return valueColumnWidth - BorderWidth - HorizontalPadding * 2f;
        }

        private static float ChildFieldLabelWidth(float valueCellWidth, float inspectorDefaultLabelWidth)
        {
            return Mathf.Min(inspectorDefaultLabelWidth, Mathf.Max(72f, valueCellWidth * 0.42f));
        }

        private static float GetValueCellContentHeight(SerializedProperty fieldProperty, float valueCellWidth)
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = ChildFieldLabelWidth(valueCellWidth, oldLabelWidth);
                if (!fieldProperty.hasVisibleChildren)
                {
                    var leafOnlyHeight = EditorGUI.GetPropertyHeight(fieldProperty, GUIContent.none);
                    return leafOnlyHeight + VerticalPadding * 2f;
                }

                var iterator = fieldProperty.Copy();
                var endProperty = iterator.GetEndProperty();
                if (!iterator.NextVisible(enterChildren: true))
                {
                    var leafHeight = EditorGUI.GetPropertyHeight(fieldProperty, GUIContent.none);
                    return leafHeight + VerticalPadding * 2f;
                }
                var inner = 0f;
                do
                {
                    var fieldLabel = new GUIContent(iterator.displayName);
                    inner += EditorGUI.GetPropertyHeight(iterator, fieldLabel);
                }
                while (iterator.NextVisible(enterChildren: false) && !SerializedProperty.EqualContents(iterator, endProperty));
                return inner + VerticalPadding * 2f;
            }
            finally
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        private static void DrawValueCellContent(Rect valueRect, SerializedProperty fieldProperty, float valueCellWidth)
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = ChildFieldLabelWidth(valueCellWidth, oldLabelWidth);
                if (!fieldProperty.hasVisibleChildren)
                {
                    EditorGUI.PropertyField(valueRect, fieldProperty, GUIContent.none);
                    return;
                }

                var iterator = fieldProperty.Copy();
                var endProperty = iterator.GetEndProperty();
                if (!iterator.NextVisible(enterChildren: true))
                {
                    EditorGUI.PropertyField(valueRect, fieldProperty, GUIContent.none);
                    return;
                }
                var y = valueRect.y;
                var w = valueRect.width;
                do
                {
                    var fieldLabel = new GUIContent(iterator.displayName);
                    var h = EditorGUI.GetPropertyHeight(iterator, fieldLabel);
                    var r = new Rect(valueRect.x, y, w, h);
                    EditorGUI.PropertyField(r, iterator, fieldLabel);
                    y += h;
                }
                while (iterator.NextVisible(enterChildren: false) && !SerializedProperty.EqualContents(iterator, endProperty));
            }
            finally
            {
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var totalHeight = BorderWidth;
            totalHeight += HeaderHeight;

            if (property.isExpanded)
            {
                totalHeight += BorderWidth;
                totalHeight += HeaderHeight;
                totalHeight += BorderWidth;

                var estimatedTableWidth = EstimateDrawerContentWidth();
                var valueCellWidth = GetValueColumnContentWidth(estimatedTableWidth);

                for(var i = 0; i < FieldNames.Length; i++)
                {
                    var fieldName = FieldNames[i];
                    var fieldProperty = property.FindPropertyRelative(fieldName);

                    if (fieldProperty != null)
                    {
                        var valueCellContentHeight = GetValueCellContentHeight(fieldProperty, valueCellWidth);
                        totalHeight += System.Math.Max(valueCellContentHeight, RowHeight);
                    }
                    else
                    {
                        totalHeight += RowHeight;
                    }

                    if (i < FieldNames.Length - 1)
                    {
                        totalHeight += BorderWidth;
                    }
                }
            }

            totalHeight += BorderWidth;

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeStyles();

            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var tableRect = position;
            var tableHeight = tableRect.height;

            var backgroundRect = new Rect(tableRect.x, tableRect.y, tableRect.width, tableHeight);
            EditorGUI.DrawRect(backgroundRect, new Color(r: 0.25f, g: 0.25f, b: 0.25f, a: 0.1f));

            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, tableRect.width, BorderWidth), _tableBorderColor);
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y + tableHeight - BorderWidth, tableRect.width, BorderWidth), _tableBorderColor);
            EditorGUI.DrawRect(new Rect(tableRect.x, tableRect.y, BorderWidth, tableHeight), _tableBorderColor);
            EditorGUI.DrawRect(new Rect(tableRect.x + tableRect.width - BorderWidth, tableRect.y, BorderWidth, tableHeight), _tableBorderColor);

            var contentX = tableRect.x + BorderWidth;
            var contentWidth = tableRect.width - BorderWidth * 2;
            var currentY = tableRect.y + BorderWidth;
            var labelWidth = contentWidth * LabelWidthRatio;
            var valueWidth = contentWidth - labelWidth;
            var valueCellWidth = valueWidth - BorderWidth - HorizontalPadding * 2;

            var firstHeaderRect = new Rect(contentX, currentY, contentWidth, HeaderHeight);
            GUI.Box(firstHeaderRect, GUIContent.none, _headerStyle);

            if (Event.current.type == EventType.MouseDown && firstHeaderRect.Contains(Event.current.mousePosition))
            {
                property.isExpanded = !property.isExpanded;
                Event.current.Use();
            }

            var foldoutRect = new Rect(firstHeaderRect.x + 4, firstHeaderRect.y, width: 12, firstHeaderRect.height);
            EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: true);

            var fieldNameRect = new Rect(firstHeaderRect.x + 16, firstHeaderRect.y, contentWidth - 24, firstHeaderRect.height);
            GUI.Label(fieldNameRect, label.text, _headerStyle);

            currentY += HeaderHeight;

            if (!property.isExpanded)
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }

            var firstHeaderBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);
            EditorGUI.DrawRect(firstHeaderBorderRect, _borderColor);
            currentY += BorderWidth;

            var secondHeaderRect = new Rect(contentX, currentY, contentWidth, HeaderHeight);
            GUI.Box(secondHeaderRect, GUIContent.none, _headerStyle);

            var headerLabelRect = new Rect(secondHeaderRect.x, secondHeaderRect.y, labelWidth, secondHeaderRect.height);
            var headerValueRect = new Rect(secondHeaderRect.x + labelWidth + BorderWidth, secondHeaderRect.y, valueWidth - BorderWidth, secondHeaderRect.height);

            GUI.Label(headerLabelRect, text: FormatCellIdName("State"), _columnHeaderStyle);

            var headerVerticalBorder = new Rect(secondHeaderRect.x + labelWidth, secondHeaderRect.y, BorderWidth, secondHeaderRect.height);
            EditorGUI.DrawRect(headerVerticalBorder, _borderColor);

            GUI.Label(headerValueRect, text: FormatCellIdName("Wrapper"), _columnHeaderStyle);

            currentY += HeaderHeight;

            var headerBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);
            EditorGUI.DrawRect(headerBorderRect, _borderColor);
            currentY += BorderWidth;

            for(var i = 0; i < FieldNames.Length; i++)
            {
                var fieldName = FieldNames[i];
                var fieldProperty = property.FindPropertyRelative(fieldName);

                if (fieldProperty == null)
                {
                    UnityEngine.Debug.LogWarning($"Field '{fieldName}' not found in WrapperForState");
                    continue;
                }

                var valueCellContentHeight = GetValueCellContentHeight(fieldProperty, valueCellWidth);
                var rowHeight = System.Math.Max(valueCellContentHeight, RowHeight);

                var rowRect = new Rect(contentX, currentY, contentWidth, rowHeight);

                if (i % 2 == 1)
                {
                    EditorGUI.DrawRect(rowRect, _stripeColor);
                }

                var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
                EditorGUI.LabelField(labelRect, FormatCellIdName(fieldName), _cellStyle);

                var verticalBorderRect = new Rect(rowRect.x + labelWidth, rowRect.y, BorderWidth, rowRect.height);
                EditorGUI.DrawRect(verticalBorderRect, _borderColor);

                var valueRect = new Rect(
                    rowRect.x + labelWidth + BorderWidth + HorizontalPadding,
                    rowRect.y + VerticalPadding,
                    valueCellWidth,
                    rowRect.height - VerticalPadding * 2);
                DrawValueCellContent(valueRect, fieldProperty, valueCellWidth);

                currentY += rowHeight;

                if (i < FieldNames.Length - 1)
                {
                    var horizontalBorderRect = new Rect(contentX, currentY, contentWidth, BorderWidth);
                    EditorGUI.DrawRect(horizontalBorderRect, _borderColor);
                    currentY += BorderWidth;
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static string FormatCellIdName(string fieldName)
        {
            var result = new StringBuilder();
            for(var i = 0; i < fieldName.Length; i++)
            {
                if (i > 0 && char.IsUpper(fieldName[i]) && !char.IsUpper(fieldName[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(fieldName[i]);
            }
            return result.ToString();
        }
    }
}
#endif
