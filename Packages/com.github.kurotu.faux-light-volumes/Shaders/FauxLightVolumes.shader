Shader "Hidden/Faux Light Volumes"
{
    Properties
    {
        // Stencil controls (configure per-material to avoid bit conflicts)
        _StencilRef ("Stencil Ref", Range(0, 255)) = 32
        _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 32
        _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 32
        // Curve control for Pass2 brightness mapping: near 1 -> 0.5, dark -> quickly to 1
        _LVGamma ("LightVolume Curve Gamma", Range(0.05, 4)) = 0.6
    }
    SubShader
    {
        // Render after opaques and before full transparents (AlphaTest+50 = 2500).
        // You can override the queue per material via CustomEditor.
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest+50" "IgnoreProjector"="True" }

        Pass
        {
            // BLEND — 2x multiply in a single pass
            // Formula: Cout = SrcFactor*Src + DstFactor*Dst = Dst*Src + Dst*Src = 2*Dst*Src
            Blend DstColor SrcColor

            // STENCIL — apply only for the first overlapping volume per pixel
            // Uses the provided Ref/Mask to avoid conflicts among different materials.
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

            #include "UnityCG.cginc"
            #include "LightVolumes.cginc"

            // Curve control: smaller -> faster rise in darks, larger -> flatter
            float _LVGamma;

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
                    // NEUTRAL PATH — keep final result equal to Dst
                    // With 2x blend, returning 0.5 guarantees Dst_new = 2*Dst*0.5 = Dst
                    return fixed4(0.5,0.5,0.5,1);
                }

                float3 L0, L1r, L1g, L1b;
                LightVolumeSH(i.worldPos, L0, L1r, L1g, L1b);
                float3 lvColor = LightVolumeEvaluate(normalize(i.worldNormal), L0, L1r, L1g, L1b);
                // PERCEPTUAL CURVE — map luminance to a scalar in [0.5, 1.0]
                // I ≈ 1 -> S ≈ 0.5 (white stays neutral overall after the 2x blend)
                // I small -> S quickly -> 1 (fast rise in dark regions)
                float I = saturate(dot(lvColor, float3(0.2126, 0.7152, 0.0722))); // luminance in linear space
                float S = 0.5 + 0.5 * pow(saturate(1.0 - I), _LVGamma);

                // COLOR PRESERVATION — keep hue by normalizing with luminance
                // H has luminance ~1; fallback to white for tiny I to avoid instability.
                float invI = (I > 1e-4) ? rcp(I) : 0.0;
                float3 H = (I > 1e-4) ? saturate(lvColor * invI) : float3(1,1,1);

                // Final colored multiplier in [0..1]. For lvColor=(1,1,1), H=(1,1,1) -> (S,S,S) => 0.5 at white.
                float3 col = saturate(H * S);
                return float4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "FauxLightVolumes.Editor.FauxLightVolumesShaderGUI"
}
