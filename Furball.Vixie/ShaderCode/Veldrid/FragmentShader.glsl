#version 450

layout(location = 0)      in vec4 fs_in_col;
layout(location = 1)      in vec2 fs_in_tex;
layout(location = 2) flat in int  fs_in_texid;

layout(set = 1, binding = 0) uniform texture2D tex_0;
layout(set = 2, binding = 0) uniform texture2D tex_1;
layout(set = 3, binding = 0) uniform texture2D tex_2;
layout(set = 4, binding = 0) uniform texture2D tex_3;
layout(set = 5, binding = 0) uniform texture2D tex_4;
layout(set = 6, binding = 0) uniform texture2D tex_5;
layout(set = 7, binding = 0) uniform texture2D tex_6;
layout(set = 8, binding = 0) uniform texture2D tex_7;

layout(set = 9, binding = 0) uniform sampler TextureSampler;

layout(location = 0) out vec4 fsout_Color;

void main() {
    switch(fs_in_texid) {
        case 0:
        fsout_Color = texture(sampler2D(tex_0, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 1:
        fsout_Color = texture(sampler2D(tex_1, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 2:
        fsout_Color = texture(sampler2D(tex_2, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 3:
        fsout_Color = texture(sampler2D(tex_3, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 4:
        fsout_Color = texture(sampler2D(tex_4, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 5:
        fsout_Color = texture(sampler2D(tex_5, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 6:
        fsout_Color = texture(sampler2D(tex_6, TextureSampler), fs_in_tex) * fs_in_col;
        break;
        case 7:
        fsout_Color = texture(sampler2D(tex_7, TextureSampler), fs_in_tex) * fs_in_col;
        break;
    }
}