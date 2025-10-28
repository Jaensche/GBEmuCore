using SDL2;
using System;

namespace GBCore.Graphics
{
    internal class Screen
    {
        private IntPtr window;
        private IntPtr renderer;

        public void Setup()
        {
            // Initilizes SDL.
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine($"There was an issue initializing SDL. {SDL.SDL_GetError()}");
            }

            // Create a new window given a title, size, and passes it a flag indicating it should be shown.
            window = SDL.SDL_CreateWindow(
                "GBEmuCore",
                SDL.SDL_WINDOWPOS_UNDEFINED,
                SDL.SDL_WINDOWPOS_UNDEFINED,
                320,
                288,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (window == IntPtr.Zero)
            {
                Console.WriteLine($"There was an issue creating the window. {SDL.SDL_GetError()}");
            }

            // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
            renderer = SDL.SDL_CreateRenderer(
                window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (renderer == IntPtr.Zero)
            {
                Console.WriteLine($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
            }
        }

        public int PollEvents()
        {
            // Check to see if there are any events and continue to do so until the queue is empty.
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        return -1;
                        
                }
            }

            return 0;
        }

        public void Render(int[,] screenBuffer)
        {
            // Sets the color that the screen will be cleared with.
            SDL.SDL_SetRenderDrawColor(renderer, 155, 188, 15, 255);

            // Clears the current render surface.
            SDL.SDL_RenderClear(renderer);

            // Set the color to red before drawing our shape
            SDL.SDL_SetRenderDrawColor(renderer, 15, 56, 15, 255);            

            // Draw a filled in rectangle.
            for (int x = 0; x < 160; x++)
            {
                for (int y = 0; y < 144; y++)
                {
                    if (screenBuffer[x, y] == 1)
                    {
                        var rect = new SDL.SDL_Rect
                        {
                            x = x*2,
                            y = y*2,
                            w = 2,
                            h = 2
                        };

                        SDL.SDL_RenderFillRect(renderer, ref rect);
                    }
                }
            }

            // Switches out the currently presented render surface with the one we just did work on.
            SDL.SDL_RenderPresent(renderer);
        }

        public void CleanUp()
        {
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }
}
