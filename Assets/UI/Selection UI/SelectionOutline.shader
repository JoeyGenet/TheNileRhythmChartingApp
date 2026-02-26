Shader "Unlit/SelectionOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SelectionColor1 ("Selection Outline Color 1", Color) = (1,1,1,1)
        _SelectionColor2 ("Selection Outline Color 1", Color) = (1,1,1,1)
        _StripeAngle ("Stripe Angle", Range(0,1)) = 0
        _StripeWidth ("Stripe Width", float) = 0.01
        _StripeFrequency ("Stripe Frequency", float) = 1
        _StripeSpeed ("Stripe Speed", float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST, _MainTex_TexelSize;
            fixed4 _SelectionColor1, _SelectionColor2;
            float _StripeWidth, _StripeFrequency, _StripeSpeed, _StripeAngle;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 textureColor = tex2D(_MainTex, i.uv);

                // invert the derivative of the y UV coord to get the ratio of pixels moved per UV
                float scale = 1/abs(ddy(i.uv.y));
                // multiply UVs by the UV-to-pixel ratio to get the UV in pixels
                // this makes the effect appear the same regardless of zoom/size of texture
                fixed2 scaledUV = i.uv * scale;

                // get a looping gradient stripe pattern
                // lerp between x and y UV values to get a diagonal stripe based on provided angle
                // multiply lerped UVs by frequency to create more stripes, add time displacement
                float stripeGradient = frac(lerp(scaledUV.x, scaledUV.y, _StripeAngle) * _StripeFrequency + _StripeSpeed*_Time.y);
                
                // stripe color based on whether pixel is above a certain brightness threshold
                fixed stripeColor = floor(stripeGradient + _StripeWidth);
                return lerp(_SelectionColor1, _SelectionColor2, stripeColor) * textureColor;
            }
            ENDCG
        }
    }
}