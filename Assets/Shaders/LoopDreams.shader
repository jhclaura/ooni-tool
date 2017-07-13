// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/LoopDreams"
{
	Properties
	{
		_Color1("Color 1", Color) = (0,0,1,0)
		_Color2("Color 2", Color) = (1,0,0,0)
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			fixed4 _Color1;
			fixed4 _Color2;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 orig : TEXCOORD1;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.orig = v.vertex;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float time = _Time.x * 100;
				float2 p = -1.0 + 2 * i.orig.yz * 100;
				p.x *= .2;
				float PI = 3.14159;

				float4 c1 = (sin(dot(p, float2(sin(time*3.0), cos(time*3.0)))*0.02 + time*3.0) + 1.0) / 2.0;
				float4 c2 = (cos(length(p)*0.03 + time) + 1.0) / 2.0;
				float4 color = (c1 + c2) / 2.0;
				float red = (cos(PI*color / 0.5 + time*3.0) + 1.0) / 2.0;

				fixed4 col = lerp(_Color1, _Color2, c2);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
