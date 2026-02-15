Shader "Custom/SimpleGrid"
{
    Properties
    {
        _GridColor ("Color de Lineas", Color) = (0.5, 0.5, 0.5, 1)
        _BackgroundColor ("Color de Fondo", Color) = (0.1, 0.1, 0.1, 1)
        _GridSize ("Densidad (Repeticiones)", Float) = 50
        _LineThickness ("Grosor de Linea", Range(0, 1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _GridColor;
            float4 _BackgroundColor;
            float _GridSize;
            float _LineThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _GridSize; // Multiplicamos UV por la densidad
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcular la cuadrícula usando la parte fraccionaria de las UVs
                float2 grid = frac(i.uv);
                
                // Determinar si estamos en una línea (cerca de 0 o 1)
                float lineX = step(grid.x, _LineThickness) + step(1.0 - _LineThickness, grid.x);
                float lineY = step(grid.y, _LineThickness) + step(1.0 - _LineThickness, grid.y);
                
                // Combinar líneas X e Y
                float isLine = max(lineX, lineY);
                
                // Mezclar colores
                return lerp(_BackgroundColor, _GridColor, isLine);
            }
            ENDCG
        }
    }
}