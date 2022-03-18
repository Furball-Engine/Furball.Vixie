#version 410
precision mediump float;

in      vec4 fs_in_col;
in      vec2 fs_in_tex;
//The texture id 
flat in int  fs_in_texid;

//The final color of the pixel
out vec4 OutputColor;

//These are the bound textures
uniform sampler2D tex_0;
uniform sampler2D tex_1;
uniform sampler2D tex_2;
uniform sampler2D tex_3;
uniform sampler2D tex_4;
uniform sampler2D tex_5;
uniform sampler2D tex_6;
uniform sampler2D tex_7;
uniform sampler2D tex_8;
uniform sampler2D tex_9;
uniform sampler2D tex_10;
uniform sampler2D tex_11;
uniform sampler2D tex_12;
uniform sampler2D tex_13;
uniform sampler2D tex_14;
uniform sampler2D tex_15;
uniform sampler2D tex_16;
uniform sampler2D tex_17;
uniform sampler2D tex_18;
uniform sampler2D tex_19;
uniform sampler2D tex_20;
uniform sampler2D tex_21;
uniform sampler2D tex_22;
uniform sampler2D tex_23;
uniform sampler2D tex_24;
uniform sampler2D tex_25;
uniform sampler2D tex_26;
uniform sampler2D tex_27;
uniform sampler2D tex_28;
uniform sampler2D tex_29;
uniform sampler2D tex_30;
uniform sampler2D tex_31;

void main() {
    if(fs_in_texid == 0) {
        OutputColor = texture(tex_0, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 1) {
        OutputColor = texture(tex_1, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 2) {
        OutputColor = texture(tex_2, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 3) {
        OutputColor = texture(tex_3, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 4) {
        OutputColor = texture(tex_4, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 5) {
        OutputColor = texture(tex_5, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 6) {
        OutputColor = texture(tex_6, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 7) {
        OutputColor = texture(tex_7, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 8) {
        OutputColor = texture(tex_8, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 9) {
        OutputColor = texture(tex_9, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 10) {
        OutputColor = texture(tex_10, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 11) {
        OutputColor = texture(tex_11, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 12) {
        OutputColor = texture(tex_12, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 13) {
        OutputColor = texture(tex_13, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 14) {
        OutputColor = texture(tex_14, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 15) {
        OutputColor = texture(tex_15, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 16) {
        OutputColor = texture(tex_16, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 17) {
        OutputColor = texture(tex_17, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 18) {
        OutputColor = texture(tex_18, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 19) {
        OutputColor = texture(tex_19, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 20) {
        OutputColor = texture(tex_20, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 21) {
        OutputColor = texture(tex_21, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 22) {
        OutputColor = texture(tex_22, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 23) {
        OutputColor = texture(tex_23, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 24) {
        OutputColor = texture(tex_24, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 25) {
        OutputColor = texture(tex_25, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 26) {
        OutputColor = texture(tex_26, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 27) {
        OutputColor = texture(tex_27, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 27) {
        OutputColor = texture(tex_27, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 28) {
        OutputColor = texture(tex_28, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 29) {
        OutputColor = texture(tex_29, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 30) {
        OutputColor = texture(tex_30, fs_in_tex) * fs_in_col;
    }
    else if(fs_in_texid == 31) {
        OutputColor = texture(tex_31, fs_in_tex) * fs_in_col;
    }
}