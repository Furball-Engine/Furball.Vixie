#version 450

layout(location = 0) in vec4 fs_in_col;

layout(location = 0) out vec4 fsout_Color;

void main() {
    fsout_Color = fs_in_col;
}
