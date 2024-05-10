#include "../../utils.h"
#include "../../portable-file-dialogs.h"

extern std::string reads(std::istream& stream);

#define read_func(bits) extern u##bits read##bits(std::istream& stream);

read_func(8)
read_func(16)
read_func(32)
read_func(64)

#undef read_func

#define bread8( s) BENDIAN(read8 (s), u8 )
#define bread16(s) BENDIAN(read16(s), u16)
#define bread32(s) BENDIAN(read32(s), u32)
#define bread64(s) BENDIAN(read64(s), u64)
#define lread8( s) LENDIAN(read8 (s), u8 )
#define lread16(s) LENDIAN(read16(s), u16)
#define lread32(s) LENDIAN(read32(s), u32)
#define lread64(s) LENDIAN(read64(s), u64)

#define readf32(s) (*(f32*)tempaddr<u32>(read32(s)))
#define readf64(s) (*(f32*)tempaddr<u64>(read64(s)))

#define breadf32(s) (*(f32*)tempaddr<u32>(bread32(s)))
#define breadf64(s) (*(f64*)tempaddr<u64>(bread64(s)))
#define lreadf32(s) (*(f32*)tempaddr<u32>(lread32(s)))
#define lreadf64(s) (*(f64*)tempaddr<u64>(lread64(s)))
