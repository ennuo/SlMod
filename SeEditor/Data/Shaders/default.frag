#version 420 core

#define rsqrt(value) pow(value, -0.5)
#define saturate(value) max(0, min(1, value))
#define lerp(a, b, w) (a + w * (b - a))

in vec2 TexCoord;
in vec3 Normal;
in vec3 Color;
in vec3 VertPos;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out int EntityID;

layout (binding = 0) uniform sampler2D gDiffuseTexture;
uniform sampler2D gNormalTexture;
uniform sampler2D gSpecularTexture;
uniform sampler2D gDetailTexture;
uniform sampler2D gEmissiveTexture;

uniform bool gHasDiffuseTexture;
uniform bool gHasNormalTexture;
uniform bool gHasSpecularTexture;
uniform bool gHasDetailTexture;
uniform bool gHasEmissiveTexture;

uniform int gEntityID;

uniform vec3 gLightAmbient = vec3(1.0);
uniform vec3 gSunColor = vec3(1.0);
uniform vec3 gSun;

layout (binding = 2, std140) uniform cbCommonModifiers
{
     vec4 gColourMul;
     vec4 gColourAdd;
     vec4 gAlphaRef;
     vec4 gFogMul;
};

const float normal_mul = 0.15;
const float normal_add = 0.5;

void main()
{
    vec3 N = normalize(Normal);
    vec3 L = normalize(-gSun);
    float diff = max(dot(N, L), 0.0);
    
    vec4 diffuse_color = vec4(1.0);
    if (gHasDiffuseTexture)
        diffuse_color = texture(gDiffuseTexture, TexCoord);
    if (gHasEmissiveTexture)
        diffuse_color += texture(gEmissiveTexture, TexCoord);
        
    diffuse_color.rgb *= Color.rgb;
    diffuse_color.rgb = (diffuse_color.rgb * gColourMul.rgb) + gColourAdd.rgb;
    
    if ((diffuse_color.w - gAlphaRef.x) < 0.0) discard;
    
    vec3 ambient = gLightAmbient * diffuse_color.rgb;
    vec3 diffuse = gSunColor * diff * diffuse_color.rgb;
    
    FragColor = vec4(diffuse_color.rgb, diffuse_color.a); 
    EntityID = gEntityID;
}