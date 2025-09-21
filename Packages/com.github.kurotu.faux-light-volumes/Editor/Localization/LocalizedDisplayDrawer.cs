using UnityEditor;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    [CustomPropertyDrawer(typeof(LocalizedLabelAttribute))]
    class LocalizedDisplayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var locAttr = (LocalizedLabelAttribute)attribute;
            // Skip relabeling array elements (Unity internally names them "data" / shows index). We only localize the parent array field.
            bool isArrayElement = property.propertyPath.EndsWith("]") && property.name == "data"; // propertyPath like LightVolumes.Array.data[0]

            GUIContent guiContent;
            if (isArrayElement)
            {
                // Use original label so Unity draws standard "Element 0" style (label.text may be null here; Unity handles default when passing 'label').
                guiContent = label;
            }
            else
            {
                // Build composite key: ClassName.PropertyName (explicit key overrides automatic)
                string baseKey = !string.IsNullOrEmpty(locAttr.Key)
                    ? locAttr.Key
                    : (property.serializedObject?.targetObject != null
                        ? property.serializedObject.targetObject.GetType().Name + "." + property.name
                        : property.name);

                string tooltipKey = string.IsNullOrEmpty(locAttr.TooltipKey) ? baseKey + ".tooltip" : locAttr.TooltipKey;

                string localizedLabel = LocalizationManager.Get(baseKey);
                string localizedTooltip = LocalizationManager.Get(tooltipKey);
                bool labelMissing = string.IsNullOrEmpty(localizedLabel) || localizedLabel == baseKey;
                bool tooltipMissing = string.IsNullOrEmpty(localizedTooltip) || localizedTooltip == tooltipKey;

                if (labelMissing && tooltipMissing)
                {
                    // Both missing: just use the original label GUIContent Unity gave us.
                    guiContent = label;
                }
                else if (labelMissing)
                {
                    // Use original label text with (possibly localized) tooltip.
                    guiContent = new GUIContent(label.text, tooltipMissing ? label.tooltip : localizedTooltip);
                }
                else
                {
                    // Have localized label; use localized tooltip if available else original.
                    guiContent = new GUIContent(localizedLabel, tooltipMissing ? label.tooltip : localizedTooltip);
                }
            }

            EditorGUI.PropertyField(position, property, guiContent, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
