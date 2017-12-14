﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	Shader "Custom/VertexColor" {
		Properties{
			_PointSize("PointSize", Float) = 1
		}

		SubShader{
			Pass{
			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct VertexInput {
			float4 v : POSITION;
			float4 color: COLOR;
		};

		float _PointSize;

		struct VertexOutput {
			float4 pos : SV_POSITION;
			float4 col : COLOR;
			float size : PSIZE;
		};

		VertexOutput vert(VertexInput v) {

			VertexOutput o;
			o.pos = UnityObjectToClipPos(v.v);
			o.col = v.color;
			o.size = _PointSize;
			return o;
		}

		float4 frag(VertexOutput o) : COLOR{
			return o.col;
		}

			ENDCG
		}
		}

	}