__constant sampler_t image_sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

//Do a 2 dimensional box blur
__kernel void box_blur(int kernel_radius, read_only image2d_t src, write_only image2d_t dst)
{
    //Get the X and Y coordinates of the pixel to edit
    int x = get_global_id(0);
    int y = get_global_id(1);
    
    //Get the width and height of the image
    int width = get_image_width(src);
    int height = get_image_height(src); 

    float4 accum = (float4)(0, 0, 0, 0);

    int kernel_width = kernel_radius * 2 + 1;
    int kernel_size = kernel_width * kernel_width;

    for(int xOffset = -kernel_radius; xOffset <= kernel_radius; xOffset++) {
        for(int yOffset = -kernel_radius; yOffset <= kernel_radius; yOffset++) {
            float4 pix = read_imagef(src, image_sampler, (int2)(x + xOffset, y + yOffset));

            accum += (float4)(
                pix.x * pix.w, 
                pix.y * pix.w, 
                pix.z * pix.w, 
                pix.w
            );
        }
    }
    
    write_imagef(dst, (int2)(x, y), (float4)(
        accum.x / kernel_size, 
        accum.y / kernel_size, 
        accum.z / kernel_size, 
        accum.w / kernel_size
    ));
}