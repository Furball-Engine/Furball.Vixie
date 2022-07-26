#version 110
precision highp float;

varying vec4 fs_in_col;

$FRAGOUT$

void main() {
    gl_FragColor = fs_in_col;
}
