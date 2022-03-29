#version 110

varying vec4 fs_in_col;
varying vec2 fs_in_tex;

//The texture2D id 
varying float fs_in_texid;

//These are the bound texture2Ds
${UNIFORMS}

void main() {
    int tex_id = int(fs_in_texid);

${IF}
}