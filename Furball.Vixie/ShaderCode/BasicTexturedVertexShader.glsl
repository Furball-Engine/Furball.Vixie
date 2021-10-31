#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;

out vec2 v_TexCoord;

uniform mat4 vx_WindowProjectionMatrix;

void main() {
    gl_Position = vx_WindowProjectionMatrix * position;

    v_TexCoord = texCoord;
}