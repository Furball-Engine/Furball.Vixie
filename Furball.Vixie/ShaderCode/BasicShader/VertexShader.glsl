#version 330 core

layout(location = 0) in vec4 position;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_Translation;

void main() {
    gl_Position = vx_WindowProjectionMatrix * u_Translation * position;
}