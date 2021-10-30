#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;

out vec2 v_TexCoord;

uniform mat4 u_ProjectionMatrix;

void main() {
    gl_Position = u_ProjectionMatrix * position;

    v_TexCoord = texCoord;
}