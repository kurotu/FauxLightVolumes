Shader "Hidden/Faux Light Volumes"
{
    Properties
    {
        // Stencil controls (configure per-material to avoid bit conflicts)
        _StencilRef ("Stencil Ref", Range(0, 255)) = 1
        _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 1
        _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 1
    }
    SubShader
    {
        Blend DstColor SrcColor // 2x Multiply
        Tags { "RenderType"="Opaque" "IgnoreProjector"="True" }

        Pass
        {
            // Ensure only the first projector applies the effect per pixel.
            // Uses the lowest stencil bit: if it's not 1 yet, draw and set it to 1; otherwise skip.
            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp NotEqual
                Pass Replace
                Fail Keep
                ZFail Keep
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "LightVolumes.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (LightVolumesEnabled() == 0) {
                    return fixed4(0.5,0.5,0.5,1); // no light volumes, return white (no change)
                }

                float3 L0, L1r, L1g, L1b;
                LightVolumeSH(i.worldPos, L0, L1r, L1g, L1b);
                float3 lvColor = LightVolumeEvaluate(normalize(i.worldNormal), L0, L1r, L1g, L1b);

                fixed4 col = fixed4(1,1,1,1);
                col.rgb *= lvColor; // apply light volume shading
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "FauxLightVolumes.Editor.FauxLightVolumesShaderGUI"
}
