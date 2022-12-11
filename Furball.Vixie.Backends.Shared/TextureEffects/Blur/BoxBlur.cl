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

    if(x >= width || y >= height || x < 0 || y < 0)
        return;

    uint4 accum = (uint4)(0, 0, 0, 0);

    int kernel_width = kernel_radius * 2 + 1;
    float kernel_size = kernel_width * kernel_width;

    for(int xOffset = -kernel_radius; xOffset <= kernel_radius; xOffset++) {
        for(int yOffset = -kernel_radius; yOffset <= kernel_radius; yOffset++) {
            uint4 pix = read_imageui(src, image_sampler, (int2)(x + xOffset, y + yOffset));
            
            accum.x += (uint)(pix.x * ((float)pix.w / 255));
            accum.y += (uint)(pix.y * ((float)pix.w / 255));
            accum.z += (uint)(pix.z * ((float)pix.w / 255));
            accum.w += pix.w;
        }
    }
    
    write_imageui(dst, (int2)(x, y), (uint4)(
        accum.x / kernel_size, 
        accum.y / kernel_size, 
        accum.z / kernel_size, 
        accum.w / kernel_size
    ));
}