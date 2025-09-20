Shader "Hidden/Faux Light Volumes"
{
    Properties
    {
        // Stencil controls (configure per-material to avoid bit conflicts)
        _StencilRef ("Stencil Ref", Range(0, 255)) = 32
        _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 32
        _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 32
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

            // Tone curve globals (set via Shader.SetGlobalFloat / SetGlobalInt)
            //  _Udon_FauxLV_Gamma      : gamma exponent (>0). If <=0, defaults to 1.0
            //  _Udon_FauxLV_CurveMode  : 0=S-curve (original), 1=standard gamma, 2=inverse gamma
            float _Udon_FauxLV_Gamma;    // replacement for legacy _Udon_LVGamma (renamed to avoid collisions)
            int   _Udon_FauxLV_CurveMode; // see modes above

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

                // LUMINANCE-BASED CORRECTION — selectable tone curve
                // Mode 0: S-curve (original) — two joined gamma curves around 0.5
                //   Below 0.5:  y = 0.5 * pow(2*I,   gamma)
                //   Above 0.5:  y = 1.0 - 0.5*pow(2*(1-I), gamma)
                // Mode 1: Standard gamma      y = pow(I, gamma)
                // Mode 2: Inverse  gamma      y = 1 - pow(1-I, gamma)
                float I = saturate(dot(lvColor, float3(0.2126, 0.7152, 0.0722))); // luminance in linear space
                float gamma = _Udon_FauxLV_Gamma;
                float S;
                if (_Udon_FauxLV_CurveMode < 0.5)
                {
                    // S-curve
                    if (I <= 0.5)
                    {
                        float t = I * 2.0; // map [0,0.5] -> [0,1]
                        S = 0.5 * pow(t, gamma);
                    }
                    else
                    {
                        float t = (1.0 - I) * 2.0; // map [0.5,1] -> [1,0]
                        S = 1.0 - 0.5 * pow(t, gamma);
                    }
                }
                else if (_Udon_FauxLV_CurveMode < 1.5)
                {
                    // Standard gamma
                    S = pow(I, gamma);
                }
                else
                {
                    // Inverse gamma
                    S = 1.0 - pow(1.0 - I, gamma);
                }

                // Preserve hue/chroma: replace luminance I with the tone curve S(I)
                // Steps: (1) Build hue vector H ≈ lvColor/I by normalizing with the inverse of I (guard near-zero I)
                //        (2) Rescale to the target luminance by multiplying H with S; clamp to [0,1] using saturate
                // Note: Simply doing lvColor*S yields luminance I·S(I) (scaling), not replacement by S(I)
                float invI = (I > 1e-4) ? rcp(I) : 0.0;                 // inverse of I (rcp); return 0 for tiny I to avoid divergence
                float3 H = (I > 1e-4) ? saturate(lvColor * invI) : float3(1,1,1); // hue vector; fallback to white when I is tiny
                float3 col = saturate(H * S);                           // replace luminance with S(I) and clamp to [0,1]
                return float4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "FauxLightVolumes.Editor.FauxLightVolumesShaderGUI"
}
