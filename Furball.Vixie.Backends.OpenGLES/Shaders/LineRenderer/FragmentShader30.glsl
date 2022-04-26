#version 300 es
precision highp float;

in vec4 fs_in_col;

layout(location = 0) out vec4 frag_color;

void main() {
    frag_color = fs_in_col;
}
