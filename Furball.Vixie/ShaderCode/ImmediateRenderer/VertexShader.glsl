#version 300 es

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;
layout(location = 2) in vec4 colorOverride;

out vec2 v_TexCoord;
out vec4 v_Color;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_RotationMatrix;
uniform float u_ModifierX;
uniform float u_ModifierY;

void main() {
    gl_Position = vx_WindowProjectionMatrix * u_RotationMatrix * position;

    v_TexCoord = vec2(clamp(texCoord.x, 0.0, 1.0), clamp(texCoord.y, 0.0, 1.0));
    v_Color = colorOverride / vec4(255, 255, 255, 255);

    // This flips the position into the coordinate space we want
    gl_Position.x *= u_ModifierX;
    gl_Position.y *= u_ModifierY;
}