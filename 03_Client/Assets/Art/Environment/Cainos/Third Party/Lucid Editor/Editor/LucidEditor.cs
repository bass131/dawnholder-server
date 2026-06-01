using Cainos.LucidEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cainos.LucidEditor
{
    public class LucidEditor : UnityEditor.Editor
    {
        protected InspectorProperty[] properties;

        internal bool hideMonoScript;
        //internal bool disableEditor;

        protected virtual void OnEnable()
        {
            hideMonoScript = target.GetType().IsDefined(typeof(HideMonoScriptAttribute), true);
            //disableEditor = target.GetType().IsDefined(typeof(DisableLucidEditorAttribute), true);
        }

        public override void OnInspectorGUI()
        {
            //if (disableEditor)
            //{
            //    base.OnInspectorGUI();
            //    return;
            //}

            serializedObject.Update();
            if (properties == null) InitializeProperties();
            ResetProperties();

            OnBeforeInspectorGUI();

            if (!hideMonoScript) LucidEditorGUILayout.ScriptField(target);
            DrawAllProperties();

            OnAfterInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected void InitializeProperties()
        {
            properties = InspectorPropertyUtil.GroupProperties(InspectorPropertyUtil.CreateProperties(serializedObject)).ToArray();
            foreach (InspectorProperty property in properties)
            {
                property.Initialize();
            }
        }

        private void ResetProperties()
        {
            foreach (InspectorProperty property in properties)
            {
                property.Reset();
            }
        }

        private void DrawAllProperties()
        {
            foreach (InspectorProperty property in properties.OrderBy(x => x.order))
            {
                property.Draw();
            }
        }

        private void OnBeforeInspectorGUI()
        {
            foreach (InspectorProperty property in properties.OrderBy(x => x.order))
            {
                property.OnBeforeInspectorGUI();
            }
        }

        private void OnAfterInspectorGUI()
        {
            foreach (InspectorProperty property in properties.OrderBy(x => x.order))
            {
                property.OnAfterInspectorGUI();
            }
        }

        //find a InspectorProperty in the editor target object by its name
        protected InspectorProperty FindProperty(string propertyName)
        {
            return FindPropertyRecursive(properties, propertyName);
        }

        // find a InspectorProperty in the editor target object by its name recursively
        private InspectorProperty FindPropertyRecursive(IEnumerable<InspectorProperty> props, string propertyName)
        {
            if (props == null) return null;
            foreach (var prop in props)
            {
                // check if this is the property we want
                if (prop.name == propertyName) return prop;

                // if this is a group, search its children
                if (prop is InspectorPropertyGroup group)
                {
                    var found = FindPropertyRecursive(group.childProperties, propertyName);
                    if (found != null) return found;
                }
            }
            return null;
        }

        //set tooltip for a given property, usually should be used in OnEnabled, will override tooltip added by the TooltipAttribute
        protected void SetTooltip( string property, string tooltip)
        {
            if (properties == null) InitializeProperties();
            var prop = FindProperty(property);
            if (prop)
            {
                prop.tooltip = tooltip;
            }
        }

    }



    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(MonoBehaviour), true)]
    //internal class MonoBehaviourEditor : LucidEditor { }

    //[CanEditMultipleObjects]
    //[CustomEditor(typeof(ScriptableObject), true)]
    //internal class ScriptableObjectEditor : LucidEditor { }

}