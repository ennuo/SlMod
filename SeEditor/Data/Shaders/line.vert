#version 330 core

layout (location = 0) in vec3 iPosition;
layout (location = 2) in vec3 iColor;

uniform mat4 gView;
uniform mat4 gProjection;

out vec3 Color;

void main()
{
    Color = iColor;
    gl_Position = gProjection * gView * vec4(iPosition, 1.0);
}