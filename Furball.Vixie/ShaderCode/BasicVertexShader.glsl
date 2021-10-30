#version 330 core

layout(location = 0) in vec4 position;

uniform mat4 u_ProjectionMatrix;

void main() {
    gl_Position = u_ProjectionMatrix * position;
}