#version 420 core

in vec2 TexCoord;
in vec3 Color;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out int EntityID;

layout (binding = 2, std140) uniform cbCommonModifiers
{
     vec4 gColourMul;
     vec4 gColourAdd;
     vec4 gAlphaRef;
     vec4 gFogMul;
};

layout (binding = 0) uniform sampler2D gDiffuseTexture;

void main()
{
    vec3 col = ((Color.rgb * texture2D(gDiffuseTexture, TexCoord).rgb) * gColourMul.rgb) + gColourAdd.rgb;
    FragColor = vec4(col.rgb, 1.0);
    EntityID = 0;
}