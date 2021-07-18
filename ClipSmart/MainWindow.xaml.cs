using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Text.RegularExpressions;
using System.Threading;


namespace ClipSmart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        UserDataContext dataContext;
        
        System.Windows.Forms.Timer timer = null;
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        static Mutex appSingleton = null;
        bool isNew = false;
        public MainWindow()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            //create a mutex so that only one process can launch

            appSingleton = new System.Threading.Mutex(false, "ClipSmart",out isNew);
            if (isNew)
            {
                InitializeComponent();
                dataContext = new UserDataContext();

                this.Left = 500;
                this.Top = 500;

                AddTimer();
                this.DataContext = dataContext;
                AddNotifyIcon();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Sorry, only one instance of ClipSmart can be ran at once.");
                Application.Current.Shutdown();
            }
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (isNew)
            {
                //release the mutex object
                Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)(() =>
                {
                    appSingleton.ReleaseMutex();
                }));
            }
            //release the mutex object here
            if (this.notifyIcon != null)
            {
                this.notifyIcon.Visible = false;
            }
        }

        private void AddTimer()
        {
            timer = new System.Windows.Forms.Timer();

            timer.Interval = 100;
            timer.Tick += new EventHandler((o, e) =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (Opacity > 0.1)
                        Opacity = Opacity - 0.1;
                    else
                    {
                        timer.Stop();
                    }
                }));
            });
        }

        private void AddNotifyIcon()
        {
            notifyIcon.Text = "Click to Re-Launch ClipSmart";
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);
            notifyIcon.Icon = Properties.Resources.Clip;
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { new System.Windows.Forms.MenuItem("Launch", new EventHandler((o, e) => { this.Opacity=1; })),
                new System.Windows.Forms.MenuItem("-"),
            new System.Windows.Forms.MenuItem("Exit", new EventHandler((o, e) => { Application.Current.Shutdown(); }))
            });
            notifyIcon.Visible = true;
        }

        void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //set the Opacity to one
            this.Opacity = 1;
        }

        IntPtr nextClipboardViewer;
        int handle;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.BottomRight.X - this.Width;
            this.Top = desktopWorkingArea.BottomRight.Y - this.Height;
            WindowInteropHelper helper = new WindowInteropHelper(this);
            handle = (int)helper.Handle;
            nextClipboardViewer = (IntPtr)SetClipboardViewer(handle);
            ClipboardChanged += new EventHandler<ClipboardChangedEventArgs>(MainWindow_ClipboardChanged);
        }

        void MainWindow_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
           IDataObject iData = e.DataObject;
           if (iData.GetDataPresent(typeof(string)))
           {
               //Add it to the collection
               string content = iData.GetData(typeof(string)) as string;
               if (content != null)
               {
                   dataContext.Add(new ClipBoardValue { CopiedMessage = content });
                   this.Left = System.Windows.SystemParameters.WorkArea.BottomRight.X - this.Width;
                   this.Top = System.Windows.SystemParameters.WorkArea.BottomRight.Y - this.Height; 
                   //start the timer to hide the pop up
                   this.Opacity = 1;
                   timer.Start();
               }
           }
        }
       
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    OnClipboardChanged();
                    SendMessage(nextClipboardViewer, msg, wParam, lParam);
                    //if (new WindowInteropHelper(this).Owner == wParam)
                    //    //set handled to true
                    //    handled = true;
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == nextClipboardViewer)
                        nextClipboardViewer = lParam;
                    else
                        SendMessage(nextClipboardViewer, msg, wParam, lParam);
                    handled = false;
                    break;

                default:
                    handled = false;
                    break;
            }
            
            return IntPtr.Zero;
        }
        /// <summary>
        /// Clipboard contents changed.
        /// </summary>
        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;


        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        void OnClipboardChanged()
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (ClipboardChanged != null)
                {
                    ClipboardChanged(this, new ClipboardChangedEventArgs(iData));
                }

            }
            catch (Exception e)
            {
                // Swallow or pop-up, not sure
                // Trace.Write(e.ToString());
                MessageBox.Show(e.ToString());
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1;
            // stop the timer
            if (timer != null)
                timer.Stop();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // start the timer
            if (timer != null)
                timer.Start();
        }
    }
    //add a extension method to observable collection to iterate through

    public class ClipboardChangedEventArgs : EventArgs
    {
        public readonly IDataObject DataObject;

        public ClipboardChangedEventArgs(IDataObject dataObject)
        {
            DataObject = dataObject;
        }
    }
}
