#include "utils.h"

s32 main() {
    std::filesystem::path assets = std::filesystem::path("assets");
    std::ofstream stream = std::ofstream("assets.bin");
    std::cout << "Building assets" << std::endl;
    for (const auto& entry : std::filesystem::recursive_directory_iterator(assets)) {
        if (entry.is_directory()) continue;
        std::filesystem::path path = std::filesystem::relative(entry.path(), assets);
        std::cout << path.string() << std::endl;
        char* data = (char*)malloc(entry.file_size());
        std::ifstream in = std::ifstream(entry.path());
        in.read(data, entry.file_size());
        in.close();
        stream.write(path.string().data(), path.string().size() + 1);
        stream.bnwrite(entry.file_size(), s32);
        stream.write(data, entry.file_size());
    }
    stream.nwrite(0, s8);
    stream.close();
    std::cout << "Done" << std::endl;
    return 0;
}