#version 140
precision mediump float;

in vec4 _Color;
in vec2 _TextureCoordinate;
flat in int _TextureId;

out vec4 OutputColor;

//These are the bound textures
uniform sampler2D tex;

void main() {
    OutputColor = texture(tex, _TextureCoordinate) * _Color;
}
