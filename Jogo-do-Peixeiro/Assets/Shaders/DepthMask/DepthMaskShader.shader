Shader "Masked/Mask" 
{
    SubShader
    {
        // Render detailed tags for better control over the pipeline.
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline" = "UniversalPipeline" }

        // O segredo principal: desenha nada nos canais de cor
        ColorMask 0
        // Escreve no Z-Buffer para enganar a renderização da água
        ZWrite On

        Pass
        {
            // O Pass deve estar vazio para não renderizar nada.
        }
    }
}