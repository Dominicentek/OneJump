#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <stdio.h>
#ifdef _WIN32
#include <Windows.h>
#else
#include <time.h>
#endif

struct KeyMap {
    const char* command;
    char code;
};

struct KeyMap keys[] = {
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
const char* secondsInput = "AD";

void print_help(const char* program_name) {
    int len = strlen(program_name);
    printf( "%s moveleft <seconds>   - Moves the character left for a specific duration\n", program_name);
    printf("%*s moveright <seconds>  - Moves the character right for a specific duration\n", len, "");
    printf("%*s jump                 - Makes the character jump\n", len, "");
    printf("%*s pickup               - Makes the character pick up/drop a cube\n", len, "");
    printf("%*s drop                 - Equivalent to \"pickup\"\n", len, "");
    printf("%*s reset                - Resets the current level\n", len, "");
    printf("%*s pause                - Pauses/resumes the game\n", len, "");
    printf("%*s keyboard             - Enables keyboard input (ruins the fun)\n", len, "");
    printf("%*s infjump              - Enables infinite jumps (you won't be able to beat the level)\n", len, "");
}

#define FLIP_BYTES(x) ({\
    union {\
        char bytes[sizeof(x)]; \
        typeof(x) value; \
    } _a, _b; \
    _a.value = x; \
    for (int _i = 0; _i < sizeof(x); _i++) {\
        _b.bytes[sizeof(x) - _i - 1] = _a.bytes[_i]; \
    } \
    _b.value; \
})

void sleep_ms(int ms) {
#ifdef _WIN32
    Sleep((DWORD)ms);
#else
    struct timespec t;
    t.tv_sec = ms / 1000;
    t.tv_nsec = (ms % 1000) * 1000000;
    nanosleep(&t, &t);
#endif
}

int main(int argc, char** argv) {
    if (argc == 1) {
        printf("Usage:\n");
        print_help(argv[0]);
        return 1;
    }
    int keymapIndex = 0;
    for (int i = 0; i < sizeof(keys) / sizeof(*keys); i++) {
        if (strcmp(argv[1], keys[i].command) == 0) {
            keymapIndex = i;
            break;
        }
    }
    if (keymapIndex == -1) {
        printf("Invalid action\n");
        print_help(argv[0]);
        return 2;
    }
    printf("%s %s\n", argv[1], argc >= 3 ? argv[2] : "");
    int key = keys[keymapIndex].code;
    bool hasSeconds = false;
    int ptr = 0;
    while (secondsInput[ptr]) {
        hasSeconds = secondsInput[ptr] == key;
        if (hasSeconds) break;
        ptr++;
    }
    int frames = 1;
    if (argc >= 3) {
        if (hasSeconds) frames = atof(argv[2]) * 60;
        else {
            printf("This action doesn't take a seconds value\n");
            print_help(argv[0]);
            return 3;
        }
    }
    if (argc == 2 && hasSeconds) {
        printf("Missing seconds argument\n");
        print_help(argv[0]);
        return 4;
    }
    const char* homepath = getenv(
#ifdef _WIN32
        "HOMEPATH"
#else
        "HOME"
#endif
    );
    char path[256];
    snprintf(path, 256, "%s/.onejump-pipe", homepath);
#ifdef _WIN32
    FILE* f = fopen(path, "wb");
#else
    FILE* f = fopen(path, "w");
#endif
    int frames_flipped = FLIP_BYTES(frames);
    fwrite(&keys[keymapIndex].code, sizeof(char), 1, f);
    fwrite(&frames_flipped, sizeof(frames), 1, f);
    fclose(f);
    sleep_ms(50);
    return 0;
}
