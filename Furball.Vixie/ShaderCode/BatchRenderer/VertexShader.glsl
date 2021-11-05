#version 330 core

layout (location = 0) in vec4 Position;
layout (location = 1) in vec2 TexCoord;
layout (location = 2) in float TexIndex;
layout (location = 3) in vec4 Color;

uniform mat4 vx_WindowProjectionMatrix;

out vec2 v_TexCoord;
out float v_TexIndex;
out vec4 v_Color;

void main() {
    v_TexCoord = TexCoord;
    v_TexIndex = TexIndex;
    v_Color = Color / vec4(255, 255, 255, 255);

    gl_Position = vx_WindowProjectionMatrix * Position;
}