#include <cstdlib>
#include <fstream>
#include "utils.h"

std::map<std::string, u8> keys = {
    { "moveleft",  'A'  },
    { "moveright", 'D'  },
    { "jump",      ' '  },
    { "pickup",    'Q'  },
    { "drop",      'Q'  },
    { "reset",     'R'  },
    { "pause",     '\e' },
    { "keyboard",  'K'  },
    { "infjump",   'J'  },
};
std::string secondsInput = "AD";

void print_help() {
    printf("game moveleft <seconds>   - Moves the character left for a specific duration\n");
    printf("     moveright <seconds>  - Moves the character right for a specific duration\n");
    printf("     jump                 - Makes the character jump\n");
    printf("     pickup               - Makes the character pick up/drop a cube\n");
    printf("     drop                 - Equivalent to \"pickup\"\n");
    printf("     reset                - Resets the current level\n");
    printf("     pause                - Pauses/resumes the game\n");
    printf("     keyboard             - Enables keyboard input (ruins the fun)\n");
    printf("     infjump              - Enables infinite jumps (you won't be able to beat the level)\n");
}

s32 main(s32 argc, char** argv) {
    if (argc == 1) {
        printf("Usage:\n");
        print_help();
        return 1;
    }
    if (keys.find(argv[1]) == keys.end()) {
        printf("Invalid action\n");
        print_help();
        return 2;
    }
    printf("%s %s\n", argv[1], argv[2]);
    s32 key = keys[argv[1]];
    bool hasSeconds = false;
    s32 ptr = 0;
    while (secondsInput[ptr]) {
        hasSeconds = secondsInput[ptr] == key;
        if (hasSeconds) break;
        ptr++;
    }
    s32 frames = 1;
    if (argc >= 3) {
        if (hasSeconds) frames = std::stof(argv[2]) * 60;
        else {
            printf("This action doesn't take a seconds value\n");
            print_help();
            return 3;
        }
    }
    if (argc == 2 && hasSeconds) {
        printf("Missing seconds argument\n");
        print_help();
        return 4;
    }
    std::string homepath = getenv(
#ifdef _WIN32
        "HOMEPATH"
#else
        "HOME"
#endif
    );
    std::ofstream stream = std::ofstream(homepath + "/.onejump-pipe", std::ios::binary);
    stream.bnwrite(key, u8);
    stream.bnwrite(frames, u32);
    stream.close();
}