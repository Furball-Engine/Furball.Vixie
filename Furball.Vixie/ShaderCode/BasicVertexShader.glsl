#version 330 core

layout(location = 0) in vec4 position;

uniform mat4 vx_WindowProjectionMatrix;

void main() {
    gl_Position = vx_WindowProjectionMatrix * position;
}