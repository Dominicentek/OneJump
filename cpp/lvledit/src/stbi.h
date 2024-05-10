extern unsigned char* stbi_load(const char* filename, int* x, int* y, int* channels_in_file, int desired_channels);
extern void stbi_image_free(void* retval_from_stbi_load);