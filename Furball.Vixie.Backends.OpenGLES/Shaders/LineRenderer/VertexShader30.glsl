#version 300 es

layout(location = 0) in vec2 a_VertexPosition;
layout(location = 1) in vec4 a_VertexColor;

uniform mat4 u_ProjectionMatrix;

out vec4 fs_in_col;

void main() {
    gl_Position = u_ProjectionMatrix * vec4(a_VertexPosition, 0, 1);
    fs_in_col = a_VertexColor;
}
