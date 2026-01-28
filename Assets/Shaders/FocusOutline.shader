Shader "Game/FocusOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 0.03
        _OutlineSoftness ("Outline Softness", Range(0, 1)) = 0.35
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 0.6)
        _GlowWidth ("Glow Width", Float) = 0.06
        _GlowSoftness ("Glow Softness", Range(0, 1)) = 0.5
        _GlowIntensity ("Glow Intensity", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+50"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            float4 _GlowColor;
            float _GlowWidth;
            float _GlowSoftness;
            float _GlowIntensity;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 smoothNormalOS : TEXCOORD3;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS : TEXCOORD0;
            float3 viewDirWS : TEXCOORD1;
        };

        float EdgeFactor(float3 normalWS, float3 viewDirWS, float softness)
        {
            float ndv = abs(dot(normalize(normalWS), normalize(viewDirWS)));
            float edge = saturate(1.0 - ndv);
            float expValue = lerp(6.0, 1.0, saturate(softness));
            return pow(edge, expValue);
        }

        float3 ResolveOutlineNormal(Attributes input)
        {
            float3 smoothNormal = input.smoothNormalOS.xyz;
            if (dot(smoothNormal, smoothNormal) > 0.0001)
            {
                return normalize(smoothNormal);
            }

            return normalize(input.normalOS);
        }

        Varyings VertCommon(Attributes input, float width)
        {
            Varyings output;
            float3 outlineNormalOS = ResolveOutlineNormal(input);
            float3 normalWS = normalize(TransformObjectToWorldNormal(outlineNormalOS));
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS += normalWS * width;
            output.positionCS = TransformWorldToHClip(positionWS);
            output.normalWS = normalWS;
            output.viewDirWS = GetWorldSpaceViewDir(positionWS);
            return output;
        }

        Varyings VertGlow(Attributes input)
        {
            return VertCommon(input, _GlowWidth);
        }

        Varyings VertOutline(Attributes input)
        {
            return VertCommon(input, _OutlineWidth);
        }
        ENDHLSL

        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma vertex VertGlow
            #pragma fragment FragGlow

            half4 FragGlow(Varyings input) : SV_Target
            {
                float alpha = EdgeFactor(input.normalWS, input.viewDirWS, _GlowSoftness);
                half3 color = _GlowColor.rgb * _GlowIntensity * alpha;
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline

            half4 FragOutline(Varyings input) : SV_Target
            {
                float alpha = EdgeFactor(input.normalWS, input.viewDirWS, _OutlineSoftness);
                half4 color = _OutlineColor;
                color.a *= alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
