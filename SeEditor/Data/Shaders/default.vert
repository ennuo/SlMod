#version 420 core

layout (location = 0) in vec3 iPosition;
layout (location = 1) in vec3 iNormal;
layout (location = 2) in vec3 iColor;
layout (location = 3) in vec2 iUV;
layout (location = 6) in vec4 iBoneWeights;
layout (location = 7) in vec4 iBones;

uniform mat4 gSkeleton[100];
uniform int gJoints[100];

uniform bool gHasColorStream;
uniform bool gIsSkinned;
uniform vec3 gColour = vec3(1.0);

out vec2 TexCoord;
out vec3 Normal;
out vec3 Color;
out vec3 VertPos;

layout (binding = 4, std140) uniform cbWorldMatrix
{
    mat4 gWorld;
};

layout (binding = 5, std140) uniform cbViewProjection
{
    mat4 gView;
    mat4 gProjection;
    mat4 gViewProjection;
    mat4 gViewInverse;
};

void main()
{
    vec4 world_pos = vec4(iPosition.xyz, 1.0);
    vec4 world_normal = vec4(iNormal.xyz, 1.0);
    
    if (gIsSkinned)
    {
        mat4 skin =
            iBoneWeights.x * gSkeleton[gJoints[int(iBones.x)]] +
            iBoneWeights.y * gSkeleton[gJoints[int(iBones.y)]] +
            iBoneWeights.z * gSkeleton[gJoints[int(iBones.z)]] +
            iBoneWeights.w * gSkeleton[gJoints[int(iBones.w)]];
            
        world_pos = skin * world_pos;
        world_normal = skin * world_normal;
    }
    
    Color = gColour;
    if (gHasColorStream) Color *= iColor;
    
    TexCoord = iUV; 
    Normal = normalize(mat3(gWorld) * world_normal.xyz);
    
    world_pos = gWorld * world_pos;
    VertPos = world_pos.xyz / world_pos.w;
    
    gl_Position = gViewProjection * world_pos;
}