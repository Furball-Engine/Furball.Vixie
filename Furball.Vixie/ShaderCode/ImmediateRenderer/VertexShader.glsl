#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;
layout(location = 2) in vec4 colorOverride;

out vec2 v_TexCoord;
out vec4 v_Color;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_RotationMatrix;

void main() {
    gl_Position = vx_WindowProjectionMatrix * u_RotationMatrix * position;

    v_TexCoord = vec2(clamp(TexCoord.x, 0.0, 1.0), clamp(TexCoord.y, 0.0, 1.0));
    v_Color = colorOverride / vec4(255, 255, 255, 255);
}