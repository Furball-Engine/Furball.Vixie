#version 110

varying vec4 fs_in_col;
varying vec2 fs_in_tex;

//The texture2D id 
varying float fs_in_texid;

//These are the bound texture2Ds
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
    int tex_id = int(fs_in_texid);
    
    if(tex_id == 0) {
        gl_FragColor = texture2D(tex_0, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 1) {
        gl_FragColor = texture2D(tex_1, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 2) {
        gl_FragColor = texture2D(tex_2, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 3) {
        gl_FragColor = texture2D(tex_3, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 4) {
        gl_FragColor = texture2D(tex_4, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 5) {
        gl_FragColor = texture2D(tex_5, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 6) {
        gl_FragColor = texture2D(tex_6, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 7) {
        gl_FragColor = texture2D(tex_7, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 8) {
        gl_FragColor = texture2D(tex_8, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 9) {
        gl_FragColor = texture2D(tex_9, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 10) {
        gl_FragColor = texture2D(tex_10, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 11) {
        gl_FragColor = texture2D(tex_11, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 12) {
        gl_FragColor = texture2D(tex_12, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 13) {
        gl_FragColor = texture2D(tex_13, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 14) {
        gl_FragColor = texture2D(tex_14, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 15) {
        gl_FragColor = texture2D(tex_15, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 16) {
        gl_FragColor = texture2D(tex_16, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 17) {
        gl_FragColor = texture2D(tex_17, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 18) {
        gl_FragColor = texture2D(tex_18, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 19) {
        gl_FragColor = texture2D(tex_19, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 20) {
        gl_FragColor = texture2D(tex_20, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 21) {
        gl_FragColor = texture2D(tex_21, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 22) {
        gl_FragColor = texture2D(tex_22, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 23) {
        gl_FragColor = texture2D(tex_23, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 24) {
        gl_FragColor = texture2D(tex_24, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 25) {
        gl_FragColor = texture2D(tex_25, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 26) {
        gl_FragColor = texture2D(tex_26, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 27) {
        gl_FragColor = texture2D(tex_27, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 27) {
        gl_FragColor = texture2D(tex_27, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 28) {
        gl_FragColor = texture2D(tex_28, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 29) {
        gl_FragColor = texture2D(tex_29, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 30) {
        gl_FragColor = texture2D(tex_30, fs_in_tex) * fs_in_col;
    }
    else if(tex_id == 31) {
        gl_FragColor = texture2D(tex_31, fs_in_tex) * fs_in_col;
    }
}