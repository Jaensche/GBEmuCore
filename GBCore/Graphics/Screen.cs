using SDL2;
using System;
using System.Runtime.InteropServices;

namespace GBCore.Graphics
{
    internal class Screen
    {
        private IntPtr window;
        private IntPtr renderer;
        private IntPtr texture;

        private int GB_SCREEN_SIZE_X = 160;
        private int GB_SCREEN_SIZE_Y = 144;

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
                GB_SCREEN_SIZE_X * 4,
                GB_SCREEN_SIZE_Y * 4,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |
                    SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

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

            texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 160, 144);

            SDL.SDL_SetTextureBlendMode(texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

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

                    case SDL.SDL_EventType.SDL_WINDOWEVENT:

                        if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                        {
                            int newW = e.window.data1;
                            int newH = e.window.data2;

                            float aspect = GB_SCREEN_SIZE_X / GB_SCREEN_SIZE_Y;

                            // Maintain the correct aspect ratio
                            float currentAspect = (float)newW / newH;
                            if (Math.Abs(currentAspect - aspect) > 0.01f)
                            {
                                if (currentAspect > aspect)
                                {
                                    // Window too wide → adjust width
                                    newW = (int)(newH * aspect);
                                }
                                else
                                {
                                    // Window too tall → adjust height
                                    newH = (int)(newW / aspect);
                                }

                                SDL.SDL_SetWindowSize(window, newW, newH);
                            }
                        }
                        break;

                }
            }

            return 0;
        }

        public void Render(int[] screenBuffer)
        {
            SDL.SDL_LockTexture(texture, IntPtr.Zero, out nint pixelsPtr, out int pitch);

            int[] rgbBuffer = ConvertToTexturePixels(screenBuffer, 160, 144);

            Marshal.Copy(rgbBuffer, 0, pixelsPtr, rgbBuffer.Length);

            SDL.SDL_UnlockTexture(texture);

            SDL.SDL_SetRenderDrawColor(renderer, 155, 188, 15, 255);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(renderer);
        }

        public void CleanUp()
        {
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }

        private int[] ConvertToTexturePixels(int[] binaryPixels, int width, int height)
        {
            int[] rgbaPixels = new int[width * height];

            for (int i = 0; i < rgbaPixels.Length; i++)
            {
                if (binaryPixels[i] == 1)
                {
                    rgbaPixels[i] = unchecked((int)0xFF0F380F);
                }
                else
                {
                    // Transparent
                    rgbaPixels[i] = unchecked(0); 
                }
            }

            return rgbaPixels;
        }        
    }
}
