#version 420 core

layout (location = 0) in vec3 iPosition;
layout (location = 2) in vec3 iColor;

layout (binding = 5, std140) uniform cbViewProjection
{
    mat4 gView;
    mat4 gProjection;
    mat4 gViewProjection;
    mat4 gViewInverse;
};

out vec3 Color;

void main()
{
    Color = iColor;
    gl_Position = gViewProjection * vec4(iPosition, 1.0);
}