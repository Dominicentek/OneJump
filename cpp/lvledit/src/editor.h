#include "utils.h"

#include <SDL2/SDL.h>

struct Tile {
    s32 index;
    std::string name;
    std::vector<u32> anim;
};

struct Entity {
    s32 index;
    std::string name;
    std::string texpath;
    std::map<std::string, u8> properties;
};

struct EntityData {
    u8 type;
    union {
        u8  ubyte;
        u16 ushort;
        s8  sbyte;
        s16 sshort;
        s32 sint;
        f32 flt;
        char bytes[4];
    } data;
};

struct EntityInstance {
    f32 x;
    f32 y;
    f32 editorX;
    f32 editorY;
    Entity* metadata;
    std::map<std::string, EntityData> properties;
};

struct TileInstance {
    s32 x;
    s32 y;
    Tile* metadata;
};

struct Level {
    std::vector<TileInstance> tiles;
    std::vector<EntityInstance> entities;
    std::vector<std::pair<int, std::vector<EntityInstance>>> wires;
};

extern std::vector<Tile> tiles;
extern std::vector<Entity> entities;

extern void set_renderer(SDL_Renderer* renderer);
extern void editor_handle_event(SDL_Event* event);
extern void editor_update();
extern void editor_render();