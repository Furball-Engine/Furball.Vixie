#version 410
#extension GL_ARB_bindless_texture : enable
#extension GL_NV_bindless_texture : enable

precision mediump float;

in      vec4 fs_in_col;
in      vec2 fs_in_tex;
//The texture sampler 
flat in sampler2D fs_in_texhandle;

//The final color of the pixel
out vec4 OutputColor;


void main() {
    OutputColor = texture(fs_in_texhandle, fs_in_tex) * fs_in_col;
    OutputColor = vec4(1, 1, 0, 1);
}