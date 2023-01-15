Shader "Hidden/CameraBloodEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloodTex ("Blood Texture", 2D) = "white" {}
        _BloodBump ("Blood Normal", 2D) = "white" {}

        _BloodAmount("Blood Amount", Range(0, 1)) = 0.0
        _Distortion("Distortion", Range(0, 2)) = 1.0
    }
         
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            //No need to alter the vertex shader as this is just an image shader
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _BloodTex;
            sampler2D _BloodBump;

            float _BloodAmount;
            float _Distortion;

            //Pixel shader == Fragment shader
            fixed4 frag (v2f i) : SV_Target
            {

                //Same sample as up, but with the blood texture 
                fixed4 bloodCol = tex2D(_BloodTex, i.uv);
                //Map the Blood amount in a range of 0 and 1
                bloodCol.a = saturate(bloodCol.a + (_BloodAmount * 2 - 1));
                
                //Calculate distortion
                half2 bump = UnpackNormal(tex2D(_BloodBump, i.uv)).xy;

                //Sample souce texture (what's in the image buffer/ what the player is seeing)
                fixed4 srcCol = tex2D(_MainTex, i.uv + bump * bloodCol.a * _Distortion);
                                
                //Modulate 2 colors (mainTex and bloodTex) together
                fixed4 overlayCol = srcCol * bloodCol * 2;
                overlayCol = lerp(srcCol, overlayCol, 0.75); //Additional Lerp between mainCol and overlayCol to display more of the mainTex

                //Blend between mainTex and blodTex and output it
                fixed4 output = lerp(srcCol, overlayCol, bloodCol.a);
                return output;
            }
            ENDCG
        }
    }
}
