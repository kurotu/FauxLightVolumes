using UnityEditor;
using UnityEngine;

namespace FauxLightVolumes.Editor
{
    public class FauxLightVolumesShaderGUI : ShaderGUI
    {
        private static readonly GUIContent StencilBitLabel = new GUIContent("Stencil Bit", "Pick a single stencil bit (0-7) to avoid conflicts; sets Ref/Read/Write masks accordingly.");
        private static readonly GUIContent AdvancedFoldoutLabel = new GUIContent("Advanced Stencil Overrides");

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Render Queue control
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            materialEditor.RenderQueueField();

            // Draw default shader properties except stencil trio (we'll manage them)
            MaterialProperty stencilRef = FindProperty("_StencilRef", properties, false);
            MaterialProperty stencilReadMask = FindProperty("_StencilReadMask", properties, false);
            MaterialProperty stencilWriteMask = FindProperty("_StencilWriteMask", properties, false);

            // Draw everything else first
            foreach (var prop in properties)
            {
                if (prop == stencilRef || prop == stencilReadMask || prop == stencilWriteMask)
                    continue;
                materialEditor.ShaderProperty(prop, prop.displayName);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Stencil", EditorStyles.boldLabel);

            // Derive current bit: prefer first set bit from read mask, else from write, else from ref.
            int currentMask = 0;
            if (stencilReadMask != null) currentMask = Mathf.RoundToInt(stencilReadMask.floatValue);
            if (currentMask == 0 && stencilWriteMask != null) currentMask = Mathf.RoundToInt(stencilWriteMask.floatValue);
            if (currentMask == 0 && stencilRef != null) currentMask = Mathf.RoundToInt(stencilRef.floatValue);

            int currentBit = 0;
            if (currentMask != 0)
            {
                // get least significant set bit index
                for (int i = 0; i < 8; i++)
                {
                    if ((currentMask & (1 << i)) != 0) { currentBit = i; break; }
                }
            }

            int newBit = EditorGUILayout.IntSlider(StencilBitLabel, currentBit, 0, 7);
            int newMask = 1 << newBit;

            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                // show computed masks (disabled)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntField("Ref (preview)", newMask);
                EditorGUILayout.IntField("ReadMask (preview)", newMask);
                EditorGUILayout.IntField("WriteMask (preview)", newMask);
                EditorGUI.EndDisabledGroup();

                if (cc.changed || newMask != currentMask)
                {
                    if (stencilRef != null) stencilRef.floatValue = newMask;
                    if (stencilReadMask != null) stencilReadMask.floatValue = newMask;
                    if (stencilWriteMask != null) stencilWriteMask.floatValue = newMask;
                }
            }

            EditorGUILayout.Space();

            // Advanced overrides: always visible
            EditorGUILayout.LabelField(AdvancedFoldoutLabel, EditorStyles.boldLabel);
            DrawIntSlider(materialEditor, stencilRef, "Stencil Ref (override)", 0, 255);
            DrawIntSlider(materialEditor, stencilReadMask, "Stencil Read Mask (override)", 0, 255);
            DrawIntSlider(materialEditor, stencilWriteMask, "Stencil Write Mask (override)", 0, 255);
        }

        private static void DrawIntSlider(MaterialEditor materialEditor, MaterialProperty prop, string label, int min, int max)
        {
            if (prop == null) return;
            int v = Mathf.Clamp(Mathf.RoundToInt(prop.floatValue), min, max);
            EditorGUI.BeginChangeCheck();
            v = EditorGUILayout.IntSlider(label, v, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                prop.floatValue = v;
            }
        }
    }
}
