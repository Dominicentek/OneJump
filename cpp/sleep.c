#include <stdlib.h>
#ifdef _WIN32
#include <Windows.h>
#else
#include <time.h>
#endif

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
    if (argc == 1) return 0;
    int ms = atof(argv[1]) * 1000;
    sleep_ms(ms);
    return 0;
}