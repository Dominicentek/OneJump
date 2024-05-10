#ifndef UTILS_H
#define UTILS_H

#include <cstdint>
#include <iostream>
#include <fstream>
#include <filesystem>
#include <map>
#include <thread>
#include <vector>

#define s8   int8_t
#define u8  uint8_t
#define s16  int16_t
#define u16 uint16_t
#define s32  int32_t
#define u32 uint32_t
#define s64  int64_t
#define u64 uint64_t
#define f32  float
#define f64  double

template<typename T> const T* tempaddr(const T& x) {
    return &x;
}

template<typename T> T flip_endianness(const T& value) {
    union {
        T val;
        char data[sizeof(T)];
    } src, dst;
    src.val = value;
    for (int i = 0; i < sizeof(T); i++) {
        dst.data[i] = src.data[sizeof(T) - 1 - i];
    }
    return dst.val;
}

#define  swrite(x)     write(x, sizeof(x))
#define  nwrite(x, t)  write((char*)tempaddr<t>(x), sizeof(t))
#define lnwrite(x, t) nwrite(LENDIAN(x, t), t)
#define bnwrite(x, t) nwrite(BENDIAN(x, t), t)

#if defined(__LITTLE_ENDIAN) || defined(__LITTLE_ENDIAN__) || defined(__ORDER_LITTLE_ENDIAN__)
#define LENDIAN(x, t) (x)
#define BENDIAN(x, t) (flip_endianness<t>(x))
#else
#define LENDIAN(x, t) (flip_endianness<t>(x))
#define BENDIAN(x, t) (x)
#endif

#define sign(x) (((x) > 0) - ((x) < 0))

#define SET_BIT(x,    o) (x)[(o) / 8] |=  (1 << (7 - (o) % 8))
#define CLR_BIT(x,    o) (x)[(o) / 8] &= ~(1 << (7 - (o) % 8))
#define GET_BIT(x,    o) (((x)[(o) / 8] >> (7 - (o) % 8)) & 1)
#define ASS_BIT(x, b, o) ((b) & 1) ? SET_BIT(x, o) : CLR_BIT(x, o)

#endif