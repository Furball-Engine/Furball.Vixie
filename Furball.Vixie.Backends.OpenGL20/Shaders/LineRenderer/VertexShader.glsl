#version 110

attribute vec2 a_VertexPosition;
attribute vec4 a_VertexColor;

uniform mat4 u_ProjectionMatrix;

varying vec4 fs_in_col;

void main() {
    gl_Position = u_ProjectionMatrix * vec4(a_VertexPosition, 0, 1);
    fs_in_col = a_VertexColor;
}
