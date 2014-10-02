using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// For SFML 2.1, you don't need SFML.System
// But for 2.2 it defines Vector2f/Vector2i
using SFML.System;
using SFML.Graphics;
using SFML.Window;
using Awesomium.Core;


namespace SFML_Web
{
    public static class BrowserManager
    {
        public static List<BrowserTab> Tabs = new List<BrowserTab>();   // List of all tabs.
        public static int CurrentTab = 0;                               // Current tabe we're on.
        public static bool Running = false;                             // Check to determine if we're in use.
        public static SynchronizationContext awesomiumContext = null;   // Used to synchronize multiple tabs.

        /// <summary>
        /// Starts the Browser Manager on another thread. Without this, no web services are available.
        /// </summary>
        public static void StartBrowserManagerService()
        {
            if (!BrowserManager.Running)
            {
                new System.Threading.Thread(() =>
                {
                    Awesomium.Core.WebCore.Started += (s, e) =>
                    {
                        BrowserManager.awesomiumContext = System.Threading.SynchronizationContext.Current;
                        Console.WriteLine("Starting Synchronization Context for Browser");
                    };
                    BrowserManager.Start();
                }).Start();
            }
        }

        /// <summary>
        /// Ends the Browser Manager. Use this when closing your application.
        /// </summary>
        public static void EndBrowserManagerService()
        {
            if (BrowserManager.Running)                         // Stops the browser manager.
            {
                BrowserManager.Close();
            }

            BrowserManager.awesomiumContext = null;             // Release the sync context.            
        }

        /// <summary>
        /// Used internally to start the service.
        /// </summary>
        private static void Start()
        {
            Console.WriteLine("Starting WebCore Engine");
            Running = true;
            WebCore.Run();
        }

        /// <summary>
        /// Creates a tab at X/Y location, a w/h size and with an INT ID and a string URL
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="url"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void NewTab(int ID, string url, int w, int h, int x, int y)
        {
            while (awesomiumContext == null)
            {
                Console.WriteLine("Context sleeping, waiting for context");
                System.Threading.Thread.Sleep(100);
            }

            foreach (BrowserTab b in Tabs)
            {
                if (b.ID == ID)
                {
                    Console.WriteLine("Tab already exists on this ID:" + ID );
                    return;
                }
            }

            awesomiumContext.Post(state =>
            {
                Console.WriteLine("Creating tab for " + url);
                Tabs.Add(new BrowserTab(ID, url, w, h, x, y));
            }, null);
        }

        /// <summary>
        /// Cleans all tabs. Use this if you want to destroy every existing tab.
        /// </summary>
        public static void Clean()
        {
            Console.WriteLine("Cleaning tabs... Count: " + Tabs.Count);

            while (Tabs.Count > 0)
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    awesomiumContext.Send(state =>
                    {
                        Tabs[i].Dispose();
                    }, null);

                    Tabs.Remove(Tabs[i]);
                }
            }

            Console.WriteLine("Tabs should be clean. Count: " + Tabs.Count);
        }

        /// <summary>
        /// Returns a specific tab.
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static BrowserTab FindTab(int ID)
        {
            foreach (BrowserTab t in Tabs)
            {
                if (t.ID == ID)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Used to destroy a tab.
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static bool DestroyTab(int ID)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (Tabs[i].ID == ID)
                {                    
                    awesomiumContext.Send(state =>
                    {
                        Tabs[i].Dispose();
                    }, null);

                    Tabs.Remove(Tabs[i]);   
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shuts down the WebManager.
        /// </summary>
        private static void Close()
        {
            Console.WriteLine("Shutting Down WebCore Engine");
            Running = false;
            Clean();
            WebCore.Shutdown();
        }
    }

    public class BrowserTab : IDisposable
    {
        public int ID = 0;              // ID of this tab.
        public string url;              // Current URL
        public bool closing = false;    // Used to close tab.
        public WebView MyTab;           // The Awesomeium WebView
        public Sprite View;             // The SFML Sprite that is returned for drawing.

        private Texture BrowserTex;     // Texture that copies the Bitmap Surface
        private BitmapSurface s;        // Bitmap surface for the Webview.
        private byte[] webBytes;        // byte array of image data buffer.

        private int browserwidth;       // Width of browesr.
        private int browserheight;      // Height of browser.

        /// <summary>
        /// Instantiates a new tab.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="URL"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public BrowserTab(int id, string URL, int w, int h, int x, int y)
        {
            ID = id;
            url = URL;

            browserwidth = w;
            browserheight = h;

            s = new BitmapSurface(w, h);
            webBytes = new byte[w * h * 4];

            BrowserTex = new Texture((uint)browserwidth, (uint)browserheight);
            View = new Sprite(BrowserTex);            
            View.Position = new Vector2f((uint)x, (uint)y);

            WebSession session = WebCore.CreateWebSession(new WebPreferences()
            {
                WebSecurity = false,
                FileAccessFromFileURL = true,
                UniversalAccessFromFileURL = true,
                LocalStorage = true,                
            });

            MyTab = WebCore.CreateWebView(browserwidth, browserheight, session, WebViewType.Offscreen);
            MyTab.Source = new Uri(URL);

            MyTab.Surface = s;

            s.Updated += (sender, e) =>
            {
                unsafe
                {
                    fixed (Byte* byteptr = webBytes)
                    {
                        s.CopyTo((IntPtr)byteptr, s.RowSpan, 4, true, false);
                        BrowserTex.Update(webBytes);
                    }
                }
            };
        }

        /// <summary>
        /// Returns the surface for drawing.
        /// </summary>
        /// <returns></returns>
        public Sprite Draw()
        {
            if (!closing)
            {                
                return View;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Move the Sprite around the screen.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Move(uint x, uint y)
        {
            View.Position = new Vector2f(x, y);
        }

        /// <summary>
        /// Used to dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Console.Write("Destroying Browser");
            s.Updated += (sender, e) => { Console.WriteLine("Destroying browser: " + url); };
            MyTab.Stop();
            MyTab.Dispose();
            Console.Write(".");
            closing = true;
            Console.Write(".");
            BrowserTex.Dispose();
            s.Dispose();
            View.Dispose();
            Console.Write(". ");
            Console.WriteLine("Browser Destroyed");
        }
    }
}
