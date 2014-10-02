using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SFML.Graphics;
using SFML.Window;

namespace SFML_Web
{
    class Program
    {
        static RenderWindow win;

        static void Main(string[] args)
        {
            win = new RenderWindow(new VideoMode(800, 600), "");
            win.Closed += (sender, e) => win.Close();
            win.KeyPressed += win_KeyPressed;
            
            BrowserManager.StartBrowserManagerService();            

            while (win.IsOpen)
            {
                win.DispatchEvents();
                win.Clear();
                DrawTabs();
                win.Display();
            }

            BrowserManager.EndBrowserManagerService();
            Environment.Exit(0);         
        }

        static void DrawTabs()
        {
            try
            {
                foreach (BrowserTab b in BrowserManager.Tabs)
                {
                    win.Draw(b.View);
                }
            }
            catch { }
        }

        static void win_KeyPressed(object sender, KeyEventArgs e)
        {            
            if (e.Code == Keyboard.Key.A)
            {
                BrowserManager.NewTab(0, "https://www.youtube.com/watch?v=ha6PP-Fw4Qw", 800, 600, 0, 0);
            }
           
            if (e.Code == Keyboard.Key.Z)
            {
                BrowserManager.DestroyTab(0);
            }           
        }
    }
}
