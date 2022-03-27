#version 450

layout(lines) in;
layout(triangle_strip, max_vertices = 4) out;

layout(set = 2, binding = 0) uniform u_viewport_sizeUniform {
    vec2 u_viewport_size;
};

layout(set = 3, binding = 0) uniform u_aa_radiusUniform {
    vec2 u_aa_radius;
};

layout(location = 0) in vec4 v_col[2];
layout(location = 1) in float v_line_width[2];

layout(location = 2) out vec4 g_col;
layout(location = 3) out float g_line_width;
layout(location = 4) out float g_line_length;
layout(location = 5) out float g_u;
layout(location = 6) out float g_v;

void main()
{
    float u_width        = u_viewport_size[0];
    float u_height       = u_viewport_size[1];
    float u_aspect_ratio = u_height / u_width;

    vec2 ndc_a = gl_in[0].gl_Position.xy / gl_in[0].gl_Position.w;
    vec2 ndc_b = gl_in[1].gl_Position.xy / gl_in[1].gl_Position.w;

    vec2 line_vector = ndc_b - ndc_a;
    vec2 viewport_line_vector = line_vector * u_viewport_size;
    vec2 dir = normalize(vec2( line_vector.x, line_vector.y * u_aspect_ratio ));

    float line_width_a     = max( 1.0, v_line_width[0] ) + u_aa_radius[0];
    float line_width_b     = max( 1.0, v_line_width[1] ) + u_aa_radius[0];
    float extension_length = u_aa_radius[1];
    float line_length      = length( viewport_line_vector ) + 2.0 * extension_length;

    vec2 normal    = vec2( -dir.y, dir.x );
    vec2 normal_a  = vec2( line_width_a/u_width, line_width_a/u_height ) * normal;
    vec2 normal_b  = vec2( line_width_b/u_width, line_width_b/u_height ) * normal;
    vec2 extension = vec2( extension_length / u_width, extension_length / u_height ) * dir;

    g_col = vec4( v_col[0].rgb, v_col[0].a * min( v_line_width[0], 1.0f ) );
    g_u = line_width_a;
    g_v = line_length * 0.5;
    g_line_width = line_width_a;
    g_line_length = line_length * 0.5;
    gl_Position = vec4( (ndc_a + normal_a - extension) * gl_in[0].gl_Position.w, gl_in[0].gl_Position.zw );
    EmitVertex();

    g_u = -line_width_a;
    g_v = line_length * 0.5;
    g_line_width = line_width_a;
    g_line_length = line_length * 0.5;
    gl_Position = vec4( (ndc_a - normal_a - extension) * gl_in[0].gl_Position.w, gl_in[0].gl_Position.zw );
    EmitVertex();

    g_col = vec4( v_col[0].rgb, v_col[0].a * min( v_line_width[0], 1.0f ) );
    g_u = line_width_b;
    g_v = -line_length * 0.5;
    g_line_width = line_width_b;
    g_line_length = line_length * 0.5;
    gl_Position = vec4( (ndc_b + normal_b + extension) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.zw );
    EmitVertex();

    g_u = -line_width_b;
    g_v = -line_length * 0.5;
    g_line_width = line_width_b;
    g_line_length = line_length * 0.5;
    gl_Position = vec4( (ndc_b - normal_b + extension) * gl_in[1].gl_Position.w, gl_in[1].gl_Position.zw );
    EmitVertex();

    EndPrimitive();
}