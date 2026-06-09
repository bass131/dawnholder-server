using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;

namespace Cainos.LucidEditor
{
    [CustomGroupProcessor(typeof(FoldoutGroupAttribute))]
    public class FoldoutGroupAttributeProcessor : PropertyGroupProcessor
    {
        private LocalPersistentData<bool> expanded;
        private AnimBool expandedAnim;

        public override void Initialize()
        {
            expanded = GetLocalPersistentData<bool>("expanded");

            // animate foldout state, expandedAnim
            expandedAnim = new AnimBool(expanded.Value);

            // scale speed by estimated size, expandedAnim
            expandedAnim.speed = GetAnimationSpeed();
            expandedAnim.valueChanged.AddListener(InternalEditorUtility.RepaintAllViews);
        }

        public override void BeginPropertyGroup()
        {
            LucidEditorGUILayout.BeginLayoutIndent(EditorGUI.indentLevel);
            bool newExpanded = LucidEditorGUILayout.BeginFoldoutGroup(expanded.Value, attribute.name, GUILayout.MinWidth(0));
            if (newExpanded != expanded.Value)
            {
                expanded.Value = newExpanded;
                expandedAnim.target = newExpanded;
            }

            group.isExpanded = expandedAnim.target;

            // keep children alive while fading out, drawFadeGroup
            bool drawFadeGroup = LucidEditorGUILayout.BeginFadeGroup(expandedAnim.faded);
            group.drawChildren = drawFadeGroup || expandedAnim.faded > 0f || expandedAnim.target;
        }

        public override void EndPropertyGroup()
        {
            LucidEditorGUILayout.EndFadeGroup();
            LucidEditorGUILayout.EndFoldoutGroup();
            LucidEditorGUILayout.EndLayoutIndent();

            EditorGUILayout.Space(2);
        }

        private float GetAnimationSpeed()
        {
            int estimatedGroupSize = Mathf.Max(1, EstimateGroupSize(group));
            float sizeFactor = Mathf.InverseLerp(1f, 12f, estimatedGroupSize);
            return Mathf.Lerp(4.5f, 1.8f, sizeFactor);
        }

        //estimate how long the group will be in the inspector by calculating the chidren count, for adjusting foldout group animation speed
        //note that this is just an estimate, it does not take child height or children in horizontal group into account
        private int EstimateGroupSize(InspectorProperty property)
        {
            InspectorPropertyGroup propertyGroup = property as InspectorPropertyGroup;
            if (propertyGroup == null) return 1;

            int count = 0;
            foreach (InspectorProperty childProperty in propertyGroup.childProperties)
            {
                count += EstimateGroupSize(childProperty);
            }

            return count;
        }
    }
}
