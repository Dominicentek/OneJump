#include "utils.h"

std::string reads(std::istream& stream) {
    char data[256], c;
    s32 ptr = 0;
    while ((c = read8(stream)) != 0 && ptr < 255) {
        data[ptr++] = c;
    }
    data[ptr] = 0;
    return data;
}

#define read_func(bits) u##bits read##bits(std::istream& stream) { \
    u##bits val;                                                    \
    stream.read((char*)&val, sizeof(val));                           \
    return val;                                                       \
}

read_func(8)
read_func(16)
read_func(32)
read_func(64)
