#version 330 core

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 TexCoord;
layout (location = 2) in float TexIndex;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_Translation;

out vec2 v_TexCoord;
out float v_TexIndex;

void main() {
    gl_Position = vx_WindowProjectionMatrix * u_Translation * position;
}