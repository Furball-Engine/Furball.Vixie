#version 330
precision mediump float;

layout(location = 0) in vec4 _Color;
layout(location = 1) in vec2 _TextureCoordinate;
layout(location = 2) flat in int _TextureId;

out vec4 OutputColor;

//These are the bound textures
${UNIFORMS}

void main() {
${IF}
}
