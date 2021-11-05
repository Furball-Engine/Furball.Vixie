#version 330 core

layout (location = 0) in vec4 Position;
layout (location = 1) in vec2 TexCoord;
layout (location = 2) in float TexIndex;

uniform mat4 vx_WindowProjectionMatrix;

out vec2 v_TexCoord;
out float v_TexIndex;

//doesnt work
//vec3 rotate(vec3 coords, float rotation, vec3 center) {
//    rotation *= 0.01745329;
//
//    mat3 zrot = mat3 (
//        cos(rotation), sin(rotation), 0.,
//        -sin(rotation), cos(rotation), 0.,
//        0., 0., 1.
//    );
//
//    vec3 newcoords = coords - center;
//
//    newcoords *= zrot;
//
//    return newcoords + center;
//}

void main() {
    v_TexCoord = TexCoord;
    v_TexIndex = TexIndex;

    gl_Position = vx_WindowProjectionMatrix * Position;
}