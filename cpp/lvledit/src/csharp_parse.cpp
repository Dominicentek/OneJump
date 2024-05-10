#include "csharp_parse.h"
#include "editor.h"
#include <cstddef>

u8* read_file(std::filesystem::path path) {
    std::ifstream stream = std::ifstream(path);
    u32 filesize = std::filesystem::file_size(path);
    u8* data = (u8*)malloc(filesize);
    stream.read((char*)data, filesize);
    stream.close();
    return data;
}

std::string between(std::string str, std::string begin, std::string end) {
    size_t from = str.find(begin) + begin.length();
    size_t to = str.find(end);
    return str.substr(from, to - from);
}

std::vector<std::string> split(std::string str, char delimiter) {
    std::vector<std::string> arr = {};
    std::string word = "";
    for (char character : (str + delimiter)) {
        if (character == delimiter) {
            arr.push_back(word);
            word = "";
        }
        else word += character;
    }
    return arr;
}

void parse_func(std::vector<std::string>* out, std::string input, char c_beg, char c_end) {
    size_t beg = input.find(c_beg);
    size_t end = input.find(c_end);
    if (beg == std::string::npos || end == std::string::npos || end <= beg) return;
    beg++;
    std::string params = input.substr(beg, end - beg);
    for (std::string param : split(params, ',')) {
        out->push_back(param);
    }
}

size_t find_func_name_end(std::string input) {
    for (size_t i = 0; i < input.length(); i++) {
        char character = input[i];
        if (character >= 'A' && character <= 'Z') continue;
        if (character >= 'a' && character <= 'z') continue;
        if (character == '_'                    ) continue;
        return i;
    }
    return input.length();
}

std::vector<std::vector<std::string>> parse_entry(std::string input, std::string prefix) {
    std::vector<std::vector<std::string>> data = {};
    size_t index = input.find(prefix);
    if (index == std::string::npos) return data;
    index += prefix.length() + 1;
    size_t length = input.find(" ", index) - index;
    data.push_back({ input.substr(index, length) });
    index += length;
    index = input.find(".", index);
    while (index != std::string::npos) {
        size_t next_index = input.find("\n", index + 1);
        std::string func = input.substr(index + 1, next_index - index);
        std::vector<std::string> parsed = {};
        size_t name_end = find_func_name_end(func);
        parsed.push_back(func.substr(0, name_end));
        parse_func(&parsed, func, '<', '>');
        parse_func(&parsed, func, '(', ')');
        data.push_back(parsed);
        index = input.find(".", next_index);
    }
    return data;
}

s32 numTiles = 0;
s32 numEntities = 0;

void parse_tile(std::string input) {
    std::vector<std::vector<std::string>> data = parse_entry(input, "public static readonly Tile");
    if (data.empty()) return;
    Tile tile;
    tile.name = data[0][0];
    tile.anim = {};
    tile.index = numTiles++;
    for (s32 i = 1; i < data.size(); i++) {
        std::vector<std::string> params = data[i];
        if (params[0] == "AddAnimFrames") {
            for (s32 j = 1; j < params.size(); j++) {
                tile.anim.push_back(std::stoi(params[j]));
            }
        }
    }
    tiles.push_back(tile);
}

void parse_entity(std::string input) {
    std::vector<std::vector<std::string>> data = parse_entry(input, "public static readonly EntityBuilder");
    if (data.empty()) return;
    Entity entity;
    entity.name = data[0][0];
    entity.index = numEntities++;
    for (s32 i = 1; i < data.size(); i++) {
        std::vector<std::string> params = data[i];
        if (params[0] == "LEHint_Texture") {
            entity.texpath = params[1].substr(1, params[1].length() - 2);
        }
        if (params[0] == "LEHint_Property") {
            u8 type = 4;
            if      (params[1] ==  "byte"  ) type = 0;
            else if (params[1] == "ushort" ) type = 1;
            else if (params[1] == "sbyte"  ) type = 2;
            else if (params[1] ==  "short" ) type = 3;
            else if (params[1] ==  "int"   ) type = 4;
            else if (params[1] ==  "float" ) type = 5;
            else std::cout << "Unsupported type: " << params[1] << "; defaulting to int" << std::endl;
            std::string name = params[2].substr(1, params[2].length() - 2);
            entity.properties.insert({ name, type });
        }
        if (params[0] == "LEHint_Hide") {
            numEntities--;
            return;
        }
    }
    entities.push_back(entity);
}

void call_parser(void(*parser)(std::string), std::string input) {
    std::vector<std::string> entries = split(input, ';');
    for (s32 i = 0; i < entries.size() - 1; i++) {
        parser(entries[i]);
    }
}

void parse_csharp_inner(std::string data) {
    std::string tiles_block = between(data, "// LE_TileBegin", "// LE_TileEnd");
    std::string entities_block = between(data, "// LE_EntityBegin", "// LE_EntityEnd");
    call_parser(parse_tile, tiles_block);
    call_parser(parse_entity, entities_block);
}

void parse_csharp() {
    u8* entity_builder_cs = read_file("../../src/engine/EntityBuilder.cs");
    u8* tiles_cs = read_file("../../src/engine/Tiles.cs");
    std::string data = std::string((char*)entity_builder_cs) + std::string((char*)tiles_cs);
    parse_csharp_inner(data);
    free(entity_builder_cs);
    free(tiles_cs);
    entities.push_back({
        .index = -1,
        .name = "Wire",
        .texpath = "images/objects/death_particle.png",
        .properties = {},
    });
}