#version 300 es
precision mediump float;

in      vec4 fs_in_col;
in      vec2 fs_in_tex;
//The texture id 
flat in int  fs_in_texid;

//The final color of the pixel
out vec4 OutputColor;

//These are the bound textures
${UNIFORMS}

void main() {
${IF}
}