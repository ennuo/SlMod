#version 330 core

layout (location = 0) in vec4 iPosition;

uniform mat4 gView;
uniform mat4 gProjection;
uniform mat4 gWorld;

void main()
{
    vec4 world_pos = gWorld * iPosition;
    gl_Position = gProjection * gView * world_pos;
}