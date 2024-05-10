#include "utils.h"
#include "editor.h"
#include "csharp_parse.h"

#include "imgui/imgui.h"
#include "imgui/imgui_impl_sdl2.h"
#include "imgui/imgui_impl_sdlrenderer2.h"

int main() {
    parse_csharp();
    SDL_SetHint(SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, "0");
    SDL_Init(SDL_INIT_VIDEO);
    SDL_DisplayMode dm;
    SDL_GetDisplayMode(0, 0, &dm);
    SDL_WindowFlags window_flags = (SDL_WindowFlags)(SDL_WINDOW_RESIZABLE | SDL_WINDOW_ALLOW_HIGHDPI);
    SDL_Window* window = SDL_CreateWindow("OneJump Level Editor", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, dm.w * 3 / 4, dm.h * 3 / 4, window_flags);
    SDL_Renderer* renderer = SDL_CreateRenderer(window, -1, SDL_RENDERER_PRESENTVSYNC | SDL_RENDERER_ACCELERATED | SDL_RENDERER_TARGETTEXTURE);
    IMGUI_CHECKVERSION();
    ImGui::CreateContext();
    ImGui::StyleColorsDark();
    ImGui::GetStyle().Colors[ImGuiCol_WindowBg].w = 1.00f;
    ImGui_ImplSDL2_InitForSDLRenderer(window, renderer);
    ImGui_ImplSDLRenderer2_Init(renderer);
    ImVec4 clear_color = ImVec4(0.25f, 0.25f, 0.25f, 1.00f);
    ImGuiIO& io = ImGui::GetIO();
    u8 open = true;
    set_renderer(renderer);
    while (open) {
        clock_t before = clock();
        SDL_Event e, *event = &e;
        while (SDL_PollEvent(event)) {
            ImGui_ImplSDL2_ProcessEvent(event);
            editor_handle_event(event);
            if (event->type == SDL_QUIT) open = false;
        }
        ImGui_ImplSDLRenderer2_NewFrame();
        ImGui_ImplSDL2_NewFrame();
        ImGui::NewFrame();
        editor_update();
        ImGui::Render();
        SDL_RenderSetScale(renderer, io.DisplayFramebufferScale.x, io.DisplayFramebufferScale.y);
        SDL_SetRenderDrawColor(renderer, (Uint8)(clear_color.x * 255), (Uint8)(clear_color.y * 255), (Uint8)(clear_color.z * 255), (Uint8)(clear_color.w * 255));
        SDL_RenderClear(renderer);
        editor_render();
        ImGui_ImplSDLRenderer2_RenderDrawData(ImGui::GetDrawData());
        SDL_RenderPresent(renderer);
        clock_t after = clock();
        if (after - before < CLOCKS_PER_SEC / 60) std::this_thread::sleep_for(std::chrono::microseconds((int)(1000000 / 60.0f - (float)(after - before) / CLOCKS_PER_SEC * 1000000)));
    }
    ImGui_ImplSDLRenderer2_Shutdown();
    ImGui_ImplSDL2_Shutdown();
    ImGui::DestroyContext();
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
