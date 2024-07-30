#version 420 core

layout (location = 0) in vec4 iPosition;

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
    vec4 world_pos = gWorld * iPosition;
    gl_Position = gViewProjection * world_pos;
}