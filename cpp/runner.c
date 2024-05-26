#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>
#ifdef _WIN32
#include <Windows.h>
#else
#include <sys/ioctl.h>
#include <unistd.h>
#include <termios.h>
#endif

#define ESC "\e"

struct MatrixInstance {
    int column;
    int position;
    int height;
    char character;
    struct MatrixInstance* next;
};

struct CmdQueue {
    char cmd[128];
    struct CmdQueue* next;
};

struct MatrixInstance* instances;
struct CmdQueue* cmd_queue;
char buffer[1024*1024];
int bufptr = 0;
int matrix_timer = 1;

int termw = 0;
int termh = 0;

struct termios settings, orig_settings;
int peek_character = -1;

const char* commands[] = {
    "moveleft <seconds>",
    "moveright <seconds>",
    "jump",
    "pickup",
    "drop",
    "reset",
    "pause",
    "keyboard",
    "infjump",
    "sleep <seconds>",
};
#define NUM_CMDS (sizeof(commands) / sizeof(*commands))

char curcmd[128];
int cmdlen = 0;
int cmdoff = 0;

int nearest_cmd = -1;
bool available_cmds[NUM_CMDS];
int longest_cmd = 0;
int num_available = 0;
int cmd_timer = 0;
int cmd_history_ptr = 0;
int cmd_history_len = 0;
char cmd_history[128][128];
int cmd_cursor_offset = 0;

void flush_buffer() {
    printf("%*s", bufptr, buffer);
    fflush(stdout);
}

#define printf(fmt...) bufptr += snprintf(buffer + bufptr, sizeof(buffer) - bufptr, fmt)

#ifdef _WIN32

#define runcmd(cmd) ShellExecuteA(NULL, "game/interface", cmd, NULL, NULL, SW_SHOWNORMAL)

void kbhit_init() {}
void kbhit_deinit() {}

#else

#define runcmd(cmd) {\
    char _cmd[256]; \
    snprintf(_cmd, 256, "game/interface %s", cmd); \
    system(_cmd); \
}

void kbhit_init() {
    tcgetattr(0, &orig_settings);
    settings = orig_settings;
    settings.c_lflag &= ~(ICANON | ECHO | ISIG);
    settings.c_cc[VMIN] = true;
    settings.c_cc[VTIME] = false;
    tcsetattr(0, TCSANOW, &settings);
}

void kbhit_deinit() {
    tcsetattr(0, TCSANOW, &orig_settings);
}

bool kbhit() {
    if (peek_character != -1) return true;
    settings.c_cc[VMIN] = false;
    tcsetattr(0, TCSANOW, &settings);
    unsigned char ch;
    int nread = read(0, &ch, 1);
    settings.c_cc[VMIN] = true;
    tcsetattr(0, TCSANOW, &settings);

    if (nread == 1) {
        peek_character = ch;
        return true;
    }
    return false;
}

int getch() {
    char ch;
    if (peek_character != -1) {
        ch = peek_character;
        peek_character = -1;
    }
    else read(0, &ch, 1);
    return ch;
}

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

void update_matrix() {
    matrix_timer--;
    struct MatrixInstance* inst = instances->next;
    int cmd_fx = 2 + cmdoff;
    int cmd_fy = termh - 1 - num_available;
    int cmd_tx = cmd_fx + longest_cmd;
    int cmd_ty = termh - 1;
    while (inst) {
        for (int i = 0; i < inst->height; i++) {
            int pos = inst->position + i;
            if (pos < 0 || pos >= termh - 1) continue;
            float color = ((i + 1.f) / inst->height) * 255;
            if (inst->column >= cmd_fx && inst->column < cmd_tx && pos >= cmd_fy && pos < cmd_ty) color /= 2;
            printf(ESC "[%d;%dH" ESC "[38;2;0;%d;0m%c", pos + 1, inst->column + 1, (int)color, inst->character);
        }
        if (matrix_timer == 0) inst->position++;
        inst = inst->next;
    }
    inst = instances->next;
    struct MatrixInstance* prev = instances;
    if (matrix_timer != 0) return;
    while (inst) {
        if (inst->position == termh - 1 || inst->column >= termw) {
            prev->next = inst->next;
            free(inst);
            inst = prev->next;
            continue;
        }
        prev = inst;
        inst = inst->next;
    }
    for (int i = 0; i < termw; i++) {
        if (rand() % 20) continue;
        struct MatrixInstance* instance = (struct MatrixInstance*)malloc(sizeof(struct MatrixInstance));
        instance->character = rand() % 95 + 32;
        instance->column = i;
        instance->height = rand() % 5 + 3;
        instance->position = -instance->height;
        instance->next = NULL;
        prev->next = instance;
        prev = instance;
    }

    matrix_timer = 5;
}

void setup_cmd_helper() {
    nearest_cmd = -1;
    longest_cmd = 0;
    num_available = 0;
    memset(available_cmds, 0, sizeof(available_cmds));
    for (int i = 0; i < NUM_CMDS; i++) {
        const char* cmd = commands[i];
        bool matches = true;
        for (int j = cmdoff; j < cmdlen; j++) {
            if ((strlen(cmd) < cmdlen - cmdoff || curcmd[j] != cmd[j - cmdoff]) || (j - cmdoff >= 2 && cmd[j - cmdoff - 2] == ' ')) {
                matches = false;
                break;
            }
        }
        if (cmdlen - cmdoff == strlen(cmd)) matches = false;
        if (matches) {
            if (longest_cmd < strlen(cmd)) longest_cmd = strlen(cmd);
            num_available++;
            available_cmds[i] = true;
            if (nearest_cmd == -1) nearest_cmd = i;
        }
    }
}

void update_cmd() {
    printf(ESC "[0m" ESC "[%d;1H> %s", termh, curcmd);
    if (nearest_cmd != -1 && cmdlen - cmdoff != 0) printf(ESC "[38;2;127;127;127m%s", commands[nearest_cmd] + cmdlen - cmdoff);
    if (kbhit()) {
        int c = 0;
        while (kbhit()) c = c * 256 + getch();
        if (c == 3 || c == 27) {
            kbhit_deinit();
            exit(0);
        }
        if ((c >= 32 && c <= 126) && cmdlen < 127) {
            for (int i = cmdlen; i >= cmdlen - cmd_cursor_offset; i--) {
                curcmd[i + 1] = curcmd[i];
            }
            curcmd[cmdlen++ - cmd_cursor_offset] = c;
            if (cmd_history_ptr == 0) memcpy(cmd_history[0], curcmd, 128);
        }
        if (c == 10) {
            struct CmdQueue* cmd = (struct CmdQueue*)malloc(sizeof(struct CmdQueue));
            struct CmdQueue* last = cmd_queue;
            while (last->next) last = last->next;
            cmd->next = NULL;
            int ptr = 0;
            for (int i = 0; i < strlen(curcmd); i++) {
                if (curcmd[i] == ';') {
                    last->next = cmd;
                    last = cmd;
                    cmd->cmd[ptr++] = 0;
                    cmd = (struct CmdQueue*)malloc(sizeof(struct CmdQueue));
                    cmd->next = NULL;
                    ptr = 0;
                    continue;
                }
                cmd->cmd[ptr++] = curcmd[i];
            }
            cmd->cmd[ptr++] = 0;
            last->next = cmd;
            if (memcmp(cmd_history[1], curcmd, 128) != 0) {
                for (int i = 127; i >= 2; i--) {
                    memcpy(cmd_history[i], cmd_history[i - 1], 128);
                }
                memcpy(cmd_history[1], curcmd, 128);
                if (cmd_history_len != 128) cmd_history_len++;
            }
            memset(curcmd, 0, 128);
            memcpy(cmd_history[0], curcmd, 128);
            cmdlen = 0;
            cmdoff = 0;
            cmd_timer = 1;
            cmd_cursor_offset = 0;
            cmd_history_ptr = 0;
        }
        if (c == 9 && nearest_cmd != -1 && cmd_cursor_offset == 0) {
            for (int i = cmdlen - cmdoff;; i++) {
                if (commands[nearest_cmd][i] == 0) break;
                curcmd[cmdlen++] = commands[nearest_cmd][i];
                if (commands[nearest_cmd][i] == ' ') break;
            }
            if (cmd_history_ptr == 0) memcpy(cmd_history[0], curcmd, 128);
        }
        if (c == 127 && cmdlen - cmd_cursor_offset > 0) {
            for (int i = cmdlen - cmd_cursor_offset - 1; i < cmdlen; i++) {
                curcmd[i] = curcmd[i + 1];
            }
            cmdlen--;
            if (cmd_history_ptr == 0) memcpy(cmd_history[0], curcmd, 128);
        }
        if (c == 1792838) cmd_cursor_offset = 0;
        if (c == 1792840) cmd_cursor_offset = cmdlen;
        if (c == 1792836 && cmd_cursor_offset < cmdlen) cmd_cursor_offset++;
        if (c == 1792835 && cmd_cursor_offset > 0) cmd_cursor_offset--;
        if (c == 1792833 && cmd_history_ptr < cmd_history_len) {
            cmd_history_ptr++;
            cmd_cursor_offset = 0;
            memcpy(curcmd, cmd_history[cmd_history_ptr], 128);
            cmdlen = strlen(curcmd);
        }
        if (c == 1792834 && cmd_history_ptr > 0) {
            cmd_history_ptr--;
            cmd_cursor_offset = 0;
            memcpy(curcmd, cmd_history[cmd_history_ptr], 128);
            cmdlen = strlen(curcmd);
        }
        cmdoff = 0;
        for (int i = cmdlen - cmd_cursor_offset - 1; i >= 0; i--) {
            if (curcmd[i] == ';') {
                cmdoff = i + 1;
                break;
            }
        }
    }
    int iter = 0;
    for (int i = 0; i < NUM_CMDS; i++) {
        if (!available_cmds[i]) continue;
        printf(ESC "[%d;%dH" ESC "[38;2;255;255;0m%s" ESC "[38;2;127;127;127m%s", termh - num_available + iter++, 3 + cmdoff, curcmd + cmdoff, commands[i] + cmdlen - cmdoff);
    }
    if (num_available == 0) printf(ESC "[%d;%dH" ESC "[38;2;255;255;0m;", termh - 1, 3 + cmdlen);
    printf(ESC "[%d;%dH", termh, cmdlen - cmd_cursor_offset + 3);
}

void run_commands() {
    cmd_timer--;
    if (cmd_timer != 0) return;
    cmd_timer = 5;
    struct CmdQueue* cmd = cmd_queue->next;
    if (!cmd) return;
    if (
        cmd->cmd[0] == 's' &&
        cmd->cmd[1] == 'l' &&
        cmd->cmd[2] == 'e' &&
        cmd->cmd[3] == 'e' &&
        cmd->cmd[4] == 'p' &&
        cmd->cmd[5] == ' '
    ) {
        cmd_timer = atof(cmd->cmd + 6) * 100;
    }
    else {
        runcmd(cmd->cmd);
    }
    cmd_queue->next = cmd->next;
    free(cmd);
}

int main() {
    srand(clock());
    instances = (struct MatrixInstance*)malloc(sizeof(struct MatrixInstance));
    cmd_queue = (struct CmdQueue*)malloc(sizeof(struct CmdQueue));
    instances->next = NULL;
    cmd_queue->next = NULL;
#ifdef _WIN32
    ShellExecuteA(NULL, "game/OneJump", "", NULL, NULL, SW_SHOWNORMAL);
#else
    popen("game/OneJump", "r");
#endif
    kbhit_init();
    while (true) {
#ifdef _WIN32
        CONSOLE_SCREEN_BUFFER_INFO csbi;
        GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &csbi);
        termw = csbi.srWindow.Right - csbi.srWindow.Left + 1;
        termh = csbi.srWindow.Bottom - csbi.srWindow.Top + 1;
#else
        struct winsize w;
        ioctl(STDOUT_FILENO, TIOCGWINSZ, &w);
        termw = w.ws_col;
        termh = w.ws_row;
#endif
        bufptr = 0;
        printf(ESC "[2J");
        setup_cmd_helper();
        update_matrix();
        update_cmd();
        run_commands();
        flush_buffer();
        sleep_ms(10);
    }
}