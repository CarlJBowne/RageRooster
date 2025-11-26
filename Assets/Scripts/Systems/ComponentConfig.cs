using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;


#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;

public class ComponentConfig
{
    private static bool showConfig = true;
    public static bool ShowConfig
    {
        get => showConfig;
        set
        {
            showConfig = value;
            OnShowConfigChanged?.Invoke(showConfig);
        }
    }
    public static Action<bool> OnShowConfigChanged;

    public static void Reset(MonoBehaviour target)
    {
        //Run through all fields with RelatedComponentAttribute

        var fields = target.GetType().GetFields();
        foreach (var field in fields)
        {
            var attrList = (Attribute[])field.GetCustomAttributes(typeof(RelatedComponentAttribute), true);
            foreach (Attribute item in attrList)
            {
                if(item is not RelatedComponentAttribute attributeValue) continue;

                var fieldType = field.FieldType;
                if (typeof(Component).IsAssignableFrom(fieldType))
                {
                    var comp = target.GetComponent(fieldType);
                    if (comp != null)
                    {
                        field.SetValue(target, comp);
                    }
                    else if (attributeValue.require)
                    {
                        Debug.LogError($"Required component of type {fieldType} not found on {target.gameObject.name}");
                    }
                }
                break;
            }
        }
    }

    [MenuItem("Tools/Toggle Component Config")]
    public static void ToggleSetup()
    {
        ShowConfig = !ShowConfig;
    }

}

/// <summary>
/// <br/> Shows this field in the editor only when the global "Show Config" option is enabled. Go to "Tools" to toggle.
/// <br/> Also adds a "Get" button to auto-assign the component from the same GameObject.
/// <br/> Also includes a "Required" toggle to mark required components and log errors if not found.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class RelatedComponentAttribute : PropertyAttribute
{
    public bool require;
    public RelatedComponentAttribute(bool require = false) { this.require = require; }

    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(RelatedComponentAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that can be shown/hidden dynamically
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.display = ComponentConfig.ShowConfig ? DisplayStyle.Flex : DisplayStyle.None;

            // WARNING ICON: appears to the left when required and null
            var icon = new Image();
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            icon.style.alignSelf = Align.Center;
            icon.tooltip = "Required";
            // Try to resolve a warning texture from common editor icon names
            Texture2D warnTex = null;
            try
            {
                warnTex = EditorGUIUtility.IconContent("console.erroricon")?.image as Texture2D
                          ?? EditorGUIUtility.IconContent("console.warn")?.image as Texture2D
                          ?? EditorGUIUtility.IconContent("console.warnicon")?.image as Texture2D
                          ?? EditorGUIUtility.FindTexture("console.warn")
                          ?? EditorGUIUtility.FindTexture("console.warnicon")
                          ?? EditorGUIUtility.IconContent("Warning")?.image as Texture2D;
            }
            catch
            {
                warnTex = null;
            }
            icon.image = warnTex;
            icon.scaleMode = ScaleMode.ScaleToFit;

            // Determine initial visibility: only if attribute.require is true AND property is null
            var relatedAttr = attribute as RelatedComponentAttribute;
            bool isRequired = relatedAttr != null && relatedAttr.require;
            bool isNull = property != null && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
            icon.style.display = (isRequired && isNull) ? DisplayStyle.Flex : DisplayStyle.None;

            // Create the default property field
            var fieldElement = new PropertyField(property);
            // Ensure the field grows to take available space
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexGrow = 1;
            fieldContainer.Add(fieldElement);

            // Create a small button to the right of the field
            var button = new Button(() => { GetComponent(property); }) { text = "Get" };
            button.style.width = 56;
            button.style.marginLeft = 4;
            button.style.flexShrink = 0;
            button.style.alignSelf = Align.Center;

            // Add the icon first so it appears to the left of the slot
            container.Add(icon);
            container.Add(fieldContainer);
            container.Add(button);

            // Contextual menu on the field: Get and Turn Off Config
            fieldElement.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Get", action => { GetComponent(property); });
                evt.menu.AppendAction("Hide Config", action => { ComponentConfig.ShowConfig = false; });
            });

            // Subscribe to global show/hide changes so the property toggles visibility dynamically
            Action<bool> handler = (visible) =>
            {
                // UIElements callbacks must run on the UI thread; setting style is fine here
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            };
            ComponentConfig.OnShowConfigChanged += handler;

            // Update icon visibility on editor updates so changes in the inspector are reflected
            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () =>
            {
                try
                {
                    bool currentlyNull = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;
                    bool shouldShowIcon = (relatedAttr != null && relatedAttr.require) && currentlyNull && ComponentConfig.ShowConfig;
                    icon.style.display = shouldShowIcon ? DisplayStyle.Flex : DisplayStyle.None;
                }
                catch
                {
                    // property can be invalid during domain reloads; ignore
                }
            };
            EditorApplication.update += updateCallback;

            // Unsubscribe when element is detached to avoid leaking the handler
            container.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                ComponentConfig.OnShowConfigChanged -= handler;
                if (updateCallback != null)
                {
                    EditorApplication.update -= updateCallback;
                    updateCallback = null;
                }
            });

            return container;
        }

        private void GetComponent(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject as MonoBehaviour;
            if (targetObject == null) return;

            Type componentType = null;

            // Preferred: use the FieldInfo provided by the PropertyDrawer
            if (fieldInfo != null)
            {
                componentType = fieldInfo.FieldType;
            }

            // Fallback: try to resolve from SerializedProperty strings
            if (componentType == null)
            {
                string typeName = null;

                // objectReferenceTypeString exists on newer Unity versions and is cleaner when available
#if UNITY_2020_1_OR_NEWER
                typeName = property.type;
#endif

                // fallback to property.type (may be "PPtr<$Rigidbody>")
                if (string.IsNullOrEmpty(typeName))
                    typeName = property.type;

                // clean names like "PPtr<$Rigidbody>" -> "Rigidbody"
                if (!string.IsNullOrEmpty(typeName) && typeName.StartsWith("PPtr<$") && typeName.EndsWith(">"))
                    typeName = typeName.Substring(6, typeName.Length - 7);

                if (!string.IsNullOrEmpty(typeName))
                {
                    // search loaded assemblies for a matching type
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var t = asm.GetType(typeName);
                            if (t == null)
                            {
                                // some types might only match by short name
                                foreach (var tt in asm.GetTypes())
                                {
                                    if (tt.Name == typeName)
                                    {
                                        t = tt;
                                        break;
                                    }
                                }
                            }
                            if (t != null)
                            {
                                componentType = t;
                                break;
                            }
                        }
                        catch
                        {
                            // ignore assemblies we can't inspect
                        }
                    }
                }
            }

            if (componentType == null)
            {
                Debug.LogError($"Unable to resolve component type for property '{property.name}' (serialized type '{property.type}').");
                return;
            }

            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                Debug.LogError($"Resolved type {componentType} is not a Component.");
                return;
            }

            var comp = targetObject.GetComponent(componentType);
            if (comp != null)
            {
                property.objectReferenceValue = comp;
                property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogError($"Component of type {componentType} not found on {targetObject.gameObject.name}");
            }
        }
    }

}

/// <summary>
/// Shows this field in the editor only when the global "Show Config" option is enabled. Go to "Tools" to toggle.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class ConfigOnlyAttribute : PropertyAttribute
{
    public ConfigOnlyAttribute() { }

    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(ConfigOnlyAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that can be shown/hidden dynamically
            var container = new VisualElement();
            container.style.display = ComponentConfig.ShowConfig ? DisplayStyle.Flex : DisplayStyle.None;
            // Create the default property field
            var fieldElement = new PropertyField(property);
            container.Add(fieldElement);
            // Subscribe to global show/hide changes so the property toggles visibility dynamically
            Action<bool> handler = (visible) =>
            {
                // UIElements callbacks must run on the UI thread; setting style is fine here
                container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            };
            ComponentConfig.OnShowConfigChanged += handler;
            // Unsubscribe when element is detached to avoid leaking the handler
            container.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                ComponentConfig.OnShowConfigChanged -= handler;
            });
            return container;
        }
    }

}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class HideAttribute : PropertyAttribute
{
    public HideAttribute() { }
    // Enable the drawer for child properties (array elements) too
    [CustomPropertyDrawer(typeof(HideAttribute), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that is always hidden
            var container = new VisualElement();
            container.style.display = DisplayStyle.None;
            return container;
        }
    }
}

#else

public class ComponentConfig
{
    public static bool ShowConfig = true;

    public static void Reset(MonoBehaviour target)
    {}
}
#endif