#version 330 core

#define rsqrt(value) pow(value, -0.5)
#define saturate(value) max(0, min(1, value))
#define lerp(a, b, w) (a + w * (b - a))

in vec2 TexCoord;
in vec3 Normal;
in vec3 Color;
in vec3 VertPos;

layout (location = 0) out vec4 FragColor;
layout (location = 1) out int EntityID;

uniform sampler2D gDiffuseTexture;
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

uniform vec3 gColourMul = vec3(1.0);
uniform vec3 gColourAdd = vec3(0.0);

uniform float gAlphaRef = 0.5;

uniform vec4 gDetailUVTransMtxU;
uniform vec4 gDetailUVTransMtxV;

uniform vec3 gViewPos;

const float normal_mul = 0.15;
const float normal_add = 0.5;

void main()
{
    vec4 color_sample = vec4(1.0);
    if (gHasDiffuseTexture)
        color_sample = texture(gDiffuseTexture, TexCoord);
        
    float alpha = color_sample.a;
    if (alpha < gAlphaRef) discard;
    
    vec3 ambient = gLightAmbient;

    vec3 light_dir = normalize(-gSun);
    vec3 normal = normalize(Normal); 
    float diff = max(dot(light_dir, normal), 0.0);
    vec3 diffuse = diff * gSunColor.rgb;
    
    vec3 view_dir = normalize(gViewPos - VertPos);
    vec3 reflect_dir = reflect(-light_dir, normal);
    
    vec3 halfway_dir = normalize(light_dir + view_dir);
    float spec = pow(max(dot(normal, halfway_dir), 0.0), 32.0);
    
    vec3 specular = gSunColor.rgb * spec;
    
    vec3 color = (ambient + diffuse + specular) * color_sample.rgb;
    
    color = pow(color, vec3(1.0 / 2.2));
    
    FragColor = vec4(color, alpha);
    EntityID = gEntityID;
}