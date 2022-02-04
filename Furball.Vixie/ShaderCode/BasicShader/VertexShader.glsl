#version 300 es

layout(location = 0) in vec4 position;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_Translation;
uniform float u_ModifierX;
uniform float u_ModifierY;

void main() {
    gl_Position = vx_WindowProjectionMatrix * u_Translation * position;
    // This flips the position into the coordinate space we want
    gl_Position.x *= u_ModifierX;
    gl_Position.y *= u_ModifierY;
}