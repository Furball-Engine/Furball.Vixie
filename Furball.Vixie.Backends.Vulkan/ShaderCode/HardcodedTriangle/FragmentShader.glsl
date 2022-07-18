#version 450

#pragma shader_stage(fragment)

layout(location = 0) out vec4 outColor;

layout(location = 0) in vec3 v_FragColor;

void main() {
    outColor = vec4(v_FragColor, 1.0);
}