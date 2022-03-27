#version 450

layout(set = 1, binding = 0) uniform u_aa_radiusUniform {
    vec2 u_aa_radius;
};

layout(location = 2) in vec4 g_col;
layout(location = 3) in float g_line_width;
layout(location = 4) in float g_line_length;
layout(location = 5) in float g_u;
layout(location = 6) in float g_v;

layout(location = 0) out vec4 frag_color;

void main()
{
    /* We render a quad that is fattened by r, giving total width of the line to be w+r. We want smoothing to happen
    around w, so that the edge is properly smoothed out. As such, in the smoothstep function we have:
    Far edge   : 1.0                                          = (w+r) / (w+r)
    Close edge : 1.0 - (2r / (w+r)) = (w+r)/(w+r) - 2r/(w+r)) = (w-r) / (w+r)
    This way the smoothing is centered around 'w'.
    */
    float au = 1.0 - smoothstep( 1.0 - ((2.0*u_aa_radius[0]) / g_line_width),  1.0, abs(g_u / g_line_width) );
    float av = 1.0 - smoothstep( 1.0 - ((2.0*u_aa_radius[1]) / g_line_length), 1.0, abs(g_v / g_line_length) );
    frag_color = g_col;
    frag_color.a *= min(av, au);
}