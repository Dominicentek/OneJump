#include "editor.h"

#include "imgui/imgui.h"
#include <SDL2/SDL_mouse.h>
#include <SDL2/SDL_render.h>
#include <cmath>
#include <cstddef>
#include <cstdio>
#include <fstream>
#include <tuple>

#define STB_IMAGE_IMPLEMENTATION
#include "../../stb_image.h"

std::vector<Tile> tiles = {};
std::vector<Entity> entities = {};

std::map<std::string, SDL_Texture*> textures = {};

Level level;

SDL_Renderer* renderer;

bool show_tools = true;
bool lock_to_grid = false;

s32 offsetX = 0;
s32 offsetY = 0;
s32 currtile = 1;
s32 currwire = -1;
s32 selwire = -1;
bool erase = false; 
Entity* currentity = nullptr;
EntityInstance* selected_entity = nullptr;
EntityInstance* grabbed_entity = nullptr;

s32 anim_timer = 0;
s32 anim = 0;

u8 file_picker = 0;
char save_file_input[256];

bool mouse_pressed;
bool mouse_down;
bool rmouse_pressed;
bool rmouse_down;

#define rect(X, Y, W, H) tempaddr<struct SDL_Rect>((struct SDL_Rect){ .x = X, .y = Y, .w = W, .h = H })

void set_renderer(SDL_Renderer* _renderer) {
    renderer = _renderer;
}

SDL_Texture* get_texture(std::string path) {
    if (textures.find(path) != textures.end()) return textures[path];
    s32 width, height, channels;
    u8* data = stbi_load(("../../assets/" + path).c_str(), &width, &height, &channels, 4);
    if (data == nullptr) return nullptr;
    SDL_Surface* surface = SDL_CreateRGBSurfaceFrom(data, width, height, 32, width * 4, 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF);
    SDL_Texture* texture = SDL_CreateTextureFromSurface(renderer, surface);
    SDL_FreeSurface(surface);
    stbi_image_free(data);
    textures.insert({ path, texture });
    return texture;
}

const char* get_type_name(u8 type) {
    switch (type) {
        case 0: return  "byte" ;
        case 1: return "ushort";
        case 2: return "sbyte" ;
        case 3: return  "short";
        case 4: return  "int"  ;
        case 5: return  "float";
    }
    return "unknown";
}

s32 tile_indexof(s32 x, s32 y) {
    for (s32 i = 0; i < level.tiles.size(); i++) {
        TileInstance& tile = level.tiles[i];
        if (tile.x == x && tile.y == y) return i;
    }
    return level.tiles.size();
}

s32 get_tile(s32 x, s32 y) {
    s32 index = tile_indexof(x, y);
    if (index == level.tiles.size()) return 0;
    return level.tiles[index].metadata->index;
}

void set_tile(s32 x, s32 y, s32 tile) {
    s32 index = tile_indexof(x, y);
    if (tile == 0) {
        if (index != level.tiles.size()) level.tiles.erase(level.tiles.begin() + index);
    }
    else if (index == level.tiles.size()) level.tiles.push_back((TileInstance){ .x = x, .y = y, .metadata = &tiles[tile] });
    else level.tiles[index].metadata = &tiles[tile];
}

SDL_Rect get_level_bounds() {
    s32 minX =  2147483647, minY =  2147483647;
    s32 maxX = -2147483648, maxY = -2147483648;
    for (TileInstance& tile : level.tiles) {
        if (tile.x < minX) minX = tile.x;
        if (tile.y < minY) minY = tile.y;
        if (tile.x > maxX) maxX = tile.x;
        if (tile.y > maxY) maxY = tile.y;
    }
    return { minX, minY, maxX, maxY };
}

void editor_handle_event(SDL_Event* event) {
    if (event->type == SDL_MOUSEBUTTONDOWN) {
        if (event->button.button == SDL_BUTTON_LEFT) mouse_down = mouse_pressed = true;
        if (event->button.button == SDL_BUTTON_RIGHT) rmouse_down = rmouse_pressed = true;
    }
    if (event->type == SDL_MOUSEBUTTONUP) {
        if (event->button.button == SDL_BUTTON_LEFT) mouse_down = false;
        if (event->button.button == SDL_BUTTON_RIGHT) rmouse_down = false;
    }
}

std::string format(const char* fmt, ...) {
    char out[BUFSIZ];
    out[0] = 0;
    va_list args;
    va_start(args, fmt);
    vsnprintf(out, BUFSIZ, fmt, args);
    va_end(args);
    return std::string(out);
}

void editor_update() {
    if (ImGui::BeginMainMenuBar()) {
        if (ImGui::BeginMenu("File")) {
            if (ImGui::MenuItem("New")) level = {};
            if (ImGui::MenuItem("Open")) {
                std::vector<std::string> file = pfd::open_file("Open level file", "", {
                    "OneJump level file", ".lvl",
                    "All files", "*"
                }).result();
                if (!file.empty()) {
                    level = {};
                    std::ifstream stream = std::ifstream(file[0], std::ios::binary);
                    s32 width = bread32(stream);
                    s32 height = bread32(stream);
                    for (s32 y = 0; y < height; y++) {
                        for (s32 x = 0; x < width; x++) {
                            set_tile(x, y, bread8(stream));
                        }
                    }
                    s32 num_entities = bread32(stream);
                    for (s32 i = 0; i < num_entities; i++) {
                        EntityInstance entity;
                        entity.metadata = &entities[bread8(stream)];
                        entity.x = entity.editorX = breadf32(stream);
                        entity.y = entity.editorY = breadf32(stream);
                        while (true) {
                            std::string prop = reads(stream);
                            if (prop == "") break;
                            EntityData property;
                            u8 type = read8(stream);
                            property.type = type;
                            property.data.sint = 0;
                            switch (type) {
                                case 2: case 0: property.data.sbyte  = (s8) bread8 (stream); break;
                                case 3: case 1: property.data.sshort = (s16)bread16(stream); break;
                                case 4: case 5: property.data.sint   = (s32)bread32(stream); break;
                            }
                            entity.properties.insert({ prop, property });
                        }
                        level.entities.push_back(entity);
                    }
                    s32 num_wires = bread32(stream);
                    for (s32 i = 0; i < num_wires; i++) {
                        level.wires.push_back({ 0, {} });
                        level.wires[level.wires.size() - 1].first = bread8(stream);
                        s32 num_points = bread32(stream);
                        for (s32 j = 0; j < num_points; j++) {
                            EntityInstance entity;
                            entity.editorX = entity.x = breadf32(stream);
                            entity.editorY = entity.y = breadf32(stream);
                            entity.metadata = &entities[entities.size() - 1];
                            entity.properties = {};
                            level.wires[level.wires.size() - 1].second.push_back(entity);
                        }
                    }
                    stream.close();
                    ImVec2 viewport = ImGui::GetMainViewport()->Size;
                    offsetX = (width  * 33 - viewport.x) / 2;
                    offsetY = (height * 33 - viewport.y) / 2;
                }
            }
            if (ImGui::MenuItem("Save")) {
                std::string file = pfd::save_file("Save level file", "", {
                    "OneJump level file", ".lvl",
                    "All files", "*"
                }).result();
                if (file == "") file = "level.lvl";
                std::ofstream stream = std::ofstream(file);
                SDL_Rect rect = get_level_bounds();
                if (rect.w < rect.x && rect.h < rect.y) {
                    stream.bnwrite(0, u32);
                    stream.bnwrite(0, u32);
                }
                else {
                    stream.bnwrite(rect.w - rect.x + 1, u32);
                    stream.bnwrite(rect.h - rect.y + 1, u32);
                    for (s32 y = rect.y; y <= rect.h; y++) {
                        for (s32 x = rect.x; x <= rect.w; x++) {
                            stream.nwrite(get_tile(x, y), u8);
                        }
                    }
                }
                stream.bnwrite(level.entities.size(), u32);
                for (EntityInstance& entity : level.entities) {
                    stream.bnwrite(entity.metadata->index, u8);
                    stream.bnwrite(entity.x - rect.x, f32);
                    stream.bnwrite(entity.y - rect.y, f32);
                    for (auto& data : entity.properties) {
                        stream.write(data.first.c_str(), data.first.length() + 1);
                        stream.nwrite(data.second.type, u8);
                        switch (data.second.type) {
                            case 0: stream.bnwrite(data.second.data.ubyte,  u8 ); break;
                            case 1: stream.bnwrite(data.second.data.ushort, u16); break;
                            case 2: stream.bnwrite(data.second.data.sbyte,  s8 ); break;
                            case 3: stream.bnwrite(data.second.data.sshort, s16); break;
                            case 4: stream.bnwrite(data.second.data.sint,   s32); break;
                            case 5: stream.bnwrite(data.second.data.flt,    f32); break;
                        }
                    }
                    stream.swrite("");
                }
                stream.bnwrite(level.wires.size(), u32);
                for (auto& wire : level.wires)  {
                    stream.bnwrite(wire.first, u8);
                    stream.bnwrite(wire.second.size(), u32);
                    for (EntityInstance& point : wire.second) {
                        stream.bnwrite(point.x, f32);
                        stream.bnwrite(point.y, f32);
                    }
                }
                stream.close();
            }
            ImGui::EndMenu();
        }
        if (ImGui::BeginMenu("Edit")) {
            if (ImGui::MenuItem("View Tools", nullptr, show_tools)) show_tools = !show_tools;
            if (ImGui::MenuItem("Lock to Grid", nullptr, lock_to_grid)) lock_to_grid = !lock_to_grid; 
            ImGui::EndMenu();
        }
        ImGui::EndMainMenuBar();
    }
    if (show_tools) { if (ImGui::Begin("Tools", &show_tools)) {
        if (ImGui::CollapsingHeader("Tiles")) {
            for (s32 i = 1; i < tiles.size(); i++) {
                if ((i - 1) % 5 != 0) ImGui::SameLine();
                SDL_Texture* texture = get_texture("images/tileset.png");
                s32 cols, rows;
                SDL_QueryTexture(texture, nullptr, nullptr, &cols, &rows);
                cols /= 16;
                rows /= 16;
                s32 t = tiles[i].anim[anim % tiles[i].anim.size()];
                f32 tx = t % 6 / (f32)cols;
                f32 ty = t / 6 / (f32)rows;
                bool selected = currtile == i;
                if (selected) ImGui::BeginDisabled();
                if (ImGui::ImageButton(
                    ("tile" + std::to_string(i)).c_str(),
                    texture,
                    ImVec2(32, 32),
                    ImVec2(tx, ty),
                    ImVec2(tx + 1.f / cols, ty + 1.f / rows)
                )) currtile = i;
                if (selected) ImGui::EndDisabled();
                if (ImGui::IsItemHovered()) {
                    ImGui::BeginTooltip();
                    ImGui::Text("%s", tiles[i].name.c_str());
                    ImGui::EndTooltip();
                }
            }
        }
        if (ImGui::CollapsingHeader("Entities")) {
            for (s32 i = 0; i < entities.size() - 1; i++) {
                if (i % 5 != 0) ImGui::SameLine();
                bool selected = &entities[i] == currentity && currtile == 0;
                if (selected) ImGui::BeginDisabled();
                SDL_Texture* texture = get_texture(entities[i].texpath);
                s32 w, h;
                SDL_QueryTexture(texture, nullptr, nullptr, &w, &h);
                if (w > h) {
                    w = 32;
                    h = 32 * (h / (f32)w);
                }
                else {
                    w = 32 * (w / (f32)h);
                    h = 32;
                }
                if (ImGui::ImageButton(
                    ("entity" + std::to_string(i)).c_str(),
                    texture,
                    ImVec2(w, h)
                )) { currentity = &entities[i]; currtile = 0; currwire = -1; }
                if (selected) ImGui::EndDisabled();
                if (ImGui::IsItemHovered()) {
                    ImGui::BeginTooltip();
                    ImGui::Text("%s", entities[i].name.c_str());
                    ImGui::EndTooltip();
                }
            }
        }
        if (currtile == 0 && selected_entity != nullptr && ImGui::CollapsingHeader("Entity Properties")) {
            if (ImGui::Button("Delete")) {
                if (currwire == -1) {
                    for (s32 i = 0; i < level.entities.size(); i++) {
                        if (&level.entities[i] == selected_entity) {
                            level.entities.erase(level.entities.begin() + i);
                            selected_entity = grabbed_entity = nullptr;
                            break;
                        }
                    }
                }
                else {
                    for (s32 i = 0; i < level.wires.size(); i++) {
                        for (s32 j = 0; j < level.wires[i].second.size(); j++) {
                            if (&level.wires[i].second[j] == selected_entity) {
                                level.wires[i].second.erase(level.wires[i].second.begin() + i);
                                selected_entity = grabbed_entity = nullptr;
                                break;
                            }
                        }
                        if (!selected_entity) break;
                    }
                }
            }
            if (selected_entity)
            if (ImGui::BeginTable("###entityprop_table", 2)) {
                for (auto& prop : selected_entity->properties) {
                    ImGui::TableNextRow();
                    ImGui::TableNextColumn();
                    ImGui::Text("%s", prop.first.c_str());
                    ImGui::TableNextColumn();
#define INPUT(min, max) ImGui::DragInt(("###" + prop.first).c_str(), (s32*)&prop.second.data, 1.f, min, max, "%d", ImGuiSliderFlags_AlwaysClamp)
                    switch (prop.second.type) {
                        case 0: INPUT(0, 255);                  prop.second.data.ubyte  = prop.second.data.sint; break;
                        case 1: INPUT(0, 65535);                prop.second.data.ushort = prop.second.data.sint; break;
                        case 2: INPUT(-128, 127);               prop.second.data.sbyte  = prop.second.data.sint; break;
                        case 3: INPUT(-32768, 32768);           prop.second.data.sshort = prop.second.data.sint; break;
                        case 4: INPUT(-2147483648, 2147483647); break;
                        case 5: ImGui::DragFloat(("###" + prop.first).c_str(), (f32*)&prop.second.data); break;
                    }
#undef INPUT
                    if (ImGui::IsItemHovered()) {
                        ImGui::BeginTooltip();
                        ImGui::Text("%s", get_type_name(prop.second.type));
                        ImGui::EndTooltip();
                    }
                }
                ImGui::EndTable();
            }
        }
        if (ImGui::CollapsingHeader("Wires")) {
            if (ImGui::Button("Add")) {
                level.wires.push_back({});
            }
            if (currwire != -1) {
                ImGui::SameLine();
                if (ImGui::Button("Delete")) {
                    level.wires.erase(level.wires.begin() + currwire);
                    selected_entity = grabbed_entity = nullptr;
                    currwire = -1;
                    currtile = 1;
                }
                ImGui::DragInt("event###wire_ev", &level.wires[currwire].first, 1.0f, 0, 255, "%d", ImGuiSliderFlags_AlwaysClamp);
            }
            selwire = -1;
            for (int i = 0; i < level.wires.size(); i++) {
                if (i % 5 != 0) ImGui::SameLine();
                ImGui::BeginDisabled(currwire == i);
                if (ImGui::Button(format("%2d", i + 1).c_str())) {
                    currentity = &entities[entities.size() - 1];
                    currtile = 0;
                    currwire = i;
                }
                if (ImGui::IsItemHovered()) selwire = i;
                ImGui::EndDisabled();
            }
        }
    } ImGui::End(); }
    anim_timer++;
    anim_timer %= 5;
    if (anim_timer == 0) anim++;
    ImGuiIO& io = ImGui::GetIO();
    if (!io.WantCaptureMouse) {
        f32 fx = (io.MousePos.x + offsetX) / 33.f;
        f32 fy = (io.MousePos.y + offsetY) / 33.f;
        s32 x = floor(fx);
        s32 y = floor(fy);
        if (currtile == 0) {
            if (mouse_pressed) {
                grabbed_entity = nullptr;
#define ENTITY_LOOP { \
                    SDL_Texture* texture = get_texture(entity.metadata->texpath); \
                    s32 w, h; \
                    SDL_QueryTexture(texture, nullptr, nullptr, &w, &h); \
                    f32 ew = w * 2 / 33.f; \
                    f32 eh = h * 2 / 33.f; \
                    f32 ex = entity.x - ew / 2; \
                    f32 ey = entity.y - eh; \
                    if (fx >= ex && fy >= ey && fx <= ex + ew && fy <= ey + eh) { \
                        selected_entity = grabbed_entity = &entity; \
                    } \
                }
                for (EntityInstance& entity : level.entities) ENTITY_LOOP
                for (auto& wire : level.wires)
                    for (EntityInstance& entity : wire.second) ENTITY_LOOP
#undef ENTITY_LOOP
                if (!grabbed_entity) {
                    EntityInstance entity;
                    entity.x = entity.editorX = fx;
                    entity.y = entity.editorY = fy;
                    entity.metadata = currentity;
                    SDL_Texture* texture = get_texture(entity.metadata->texpath);
                    s32 h;
                    SDL_QueryTexture(texture, nullptr, nullptr, nullptr, &h);
                    entity.y += h / 33.f;
                    entity.editorY = entity.y;
                    for (auto& entry : currentity->properties) {
                        EntityData data;
                        data.type = entry.second;
                        memset(&data.data, 0, sizeof(data.data));
                        entity.properties.insert({ entry.first, data });
                    }
                    if (currwire == -1) {
                        level.entities.push_back(entity);
                        selected_entity = grabbed_entity = &level.entities[level.entities.size() - 1];
                    }
                    else {
                        level.wires[currwire].second.push_back(entity);
                        selected_entity = grabbed_entity = &level.wires[currwire].second[level.wires[currwire].second.size() - 1];
                    }
                }
            }
            if (mouse_down) {
                if (!lock_to_grid) {
                    grabbed_entity->editorX = grabbed_entity->x;
                    grabbed_entity->editorY = grabbed_entity->y;
                }
                grabbed_entity->editorX += (io.MousePos.x - io.MousePosPrev.x) / 33.f;
                grabbed_entity->editorY += (io.MousePos.y - io.MousePosPrev.y) / 33.f;
                if (lock_to_grid) {
                    grabbed_entity->x = floor(grabbed_entity->editorX * 2) / 2.f;
                    grabbed_entity->y = floor(grabbed_entity->editorY * 2) / 2.f;
                }
                else {
                    grabbed_entity->x = grabbed_entity->editorX;
                    grabbed_entity->y = grabbed_entity->editorY;
                }
            }
        }
        else {
            if (mouse_pressed) erase = get_tile(x, y);
            if (mouse_down) set_tile(x, y, erase ? 0 : currtile);
        }
        if (rmouse_down) {
            offsetX -= io.MousePos.x - io.MousePosPrev.x;
            offsetY -= io.MousePos.y - io.MousePosPrev.y;
        }
    }
    mouse_pressed = rmouse_pressed = false;
}

void editor_render() {
    SDL_SetRenderDrawColor(renderer, 127, 127, 127, 127);
    ImVec2 viewport = ImGui::GetMainViewport()->WorkSize;
    s32 fromX = floor(offsetX / 33.f);
    s32 fromY = floor(offsetY / 33.f);
    s32   toX = fromX + ceil(viewport.x / 33.f);
    s32   toY = fromY + ceil(viewport.y / 33.f);
    for (s32 x = fromX; x <= toX; x++) {
        for (s32 y = fromY; y <= toY; y++) {
            s32 index = get_tile(x, y);
            s32 tile = tiles[index].anim[anim % tiles[index].anim.size()];
            s32 tx = tile % 6;
            s32 ty = tile / 6;
            SDL_RenderDrawRect(renderer, rect(x * 33 - offsetX, y * 33 - offsetY, 34, 34));
            SDL_RenderCopy(renderer, get_texture("images/tileset.png"),
                rect(tx * 16, ty * 16, 16, 16),
                rect(x * 33 - offsetX + 1, y * 33 - offsetY + 1, 32, 32)
            );
        }
    }
    for (s32 i = 0; i < level.wires.size(); i++) {
        if (i == currwire) SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
        else if (i == selwire) SDL_SetRenderDrawColor(renderer, 192, 192, 192, 255);
        else SDL_SetRenderDrawColor(renderer, 128, 128, 128, 255);
        for (s32 j = 0; j < (s32) level.wires[i].second.size() - 1; j++) {
            SDL_RenderDrawLineF(renderer,
                level.wires[i].second[j    ].x * 33 - offsetX, level.wires[i].second[j    ].y * 33 - offsetY,
                level.wires[i].second[j + 1].x * 33 - offsetX, level.wires[i].second[j + 1].y * 33 - offsetY
            );
        }
    }
    SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);
#define ENTITY_LOOP { \
        SDL_Texture* texture = get_texture(entity.metadata->texpath); \
        s32 w, h; \
        SDL_QueryTexture(texture, nullptr, nullptr, &w, &h); \
        s32 x = entity.x * 33 - offsetX - w; \
        s32 y = entity.y * 33 - offsetY - h * 2; \
        SDL_RenderCopy(renderer, texture, nullptr, rect(x, y, w * 2, h * 2)); \
    }
    for (EntityInstance& entity : level.entities) ENTITY_LOOP
    for (auto& wire : level.wires)
        for (EntityInstance& entity : wire.second) ENTITY_LOOP
#undef ENTITY_LOOP
}