Shader "Educational/UndergroundPalette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _DarkColor ("Sombra subterránea", Color) = (0.015,0.03,0.07,1)
        _LightColor ("Luz subterránea", Color) = (0.4,0.88,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment PaletteFrag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            fixed4 _DarkColor;
            fixed4 _LightColor;

            // Cambiar la paleta conserva la silueta y animación de los PNG existentes.
            fixed4 PaletteFrag(v2f input) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(input.texcoord);
                fixed brightness = dot(source.rgb, fixed3(0.299, 0.587, 0.114));
                fixed4 color = lerp(_DarkColor, _LightColor, brightness);
                color.a = source.a * input.color.a;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
