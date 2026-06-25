Shader "Custom/SprayBrush"
{
    Properties
    {
        _MainTex ("Previous Paint", 2D) = "black" {}
        _BrushColor ("Color", Color) = (1,0,0,1)
        _BrushWorldPos ("World Pos", Vector) = (0,0,0,0)
        _BrushDir ("Spray Dir", Vector) = (0,0,1,0)
        _BrushSize ("Size", Float) = 0.15
        _BrushHardness ("Hardness", Float) = 0.4
        _BrushFlow ("Flow", Float) = 0.15
    }
    SubShader
    {
        Cull Off ZTest Always ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BrushColor, _BrushWorldPos, _BrushDir;
            float _BrushSize, _BrushHardness, _BrushFlow;
            float4x4 _ObjToWorld;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNrm : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float2 uvClip = v.uv * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                    uvClip.y = -uvClip.y;
                #endif
                o.pos = float4(uvClip, 0.0, 1.0);
                o.uv = v.uv;
                o.worldPos = mul(_ObjToWorld, v.vertex).xyz;
                o.worldNrm = normalize(mul((float3x3)_ObjToWorld, v.normal));
                return o;
            }

            float hash(float3 p)
            {
                return frac(sin(dot(p, float3(12.99,78.23,37.71)))*43758.5);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 prev = tex2D(_MainTex, i.uv);

                float d = distance(i.worldPos, _BrushWorldPos.xyz);
                float mask = 1.0 - smoothstep(_BrushSize * _BrushHardness,
                                              _BrushSize, d);

                float facing = saturate(dot(i.worldNrm, -_BrushDir.xyz));
                mask *= facing;

                mask *= 0.65 + 0.35 * hash(floor(i.worldPos * 220.0));

                float add  = mask * _BrushFlow;
                float newA = saturate(prev.a + add);

                fixed4 o;
                o.rgb = newA > 1e-4
                    ? (prev.rgb * prev.a + _BrushColor.rgb * add) / newA
                    : _BrushColor.rgb;
                o.a = newA;
                return o;
            }
            ENDCG
        }
    }
}