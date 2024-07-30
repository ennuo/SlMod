#version 420 core

layout (location = 0) in vec3 iPosition;
layout (location = 2) in vec3 iColor;
layout (location = 3) in vec2 iUV;

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

out vec2 TexCoord;
out vec3 Color;

void main()
{
    Color = iColor;
    TexCoord = iUV;
    
    vec4 world_pos = gWorld * vec4(iPosition.xyz, 1.0);
    gl_Position = gViewProjection * world_pos;
}