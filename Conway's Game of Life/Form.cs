using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace Conway_s_Game_of_Life
{
    public partial class Form : System.Windows.Forms.Form
    {
        public Form()
        {
            InitializeComponent();
        }

        //Global variables
        byte[][] map_0;
        byte[][] map_1;
        int map_index;
        int x = 0;
        int y = 0;
        Bitmap bitmap;
        Bitmap bitmap_resized;
        Rectangle b_rectangle;
        public bool resize_to_fit = true;

        //Constants
        public const string SETTINGS_FILE = "settings.ini";

        /// <summary>
        /// Function calculating next grid state
        /// </summary>
        /// <param name="array_input">Byte array containing current grid state</param>
        /// <param name="array_output">Byte array that will contain the next grid state</param>
        /// <param name="x">Width of the array</param>
        /// <param name="y">Height of the array</param>
        void calculate(byte[][] array_input, byte[][] array_output, int x, int y)
        {
            //Using parallel computation to increase speed
            Parallel.For(1, x - 1, i =>
            {
                for (int j = 1; j < y - 1; j++)
                {
                    int neighbours = 0;

                    for (int k = -1; k < 2; k++)
                    {
                        for (int l = -1; l < 2; l++)
                        {
                            if (k == 0 && l == 0) continue;

                            if (array_input[i + k][j + l] == 1) neighbours++;
                        }
                    }

                    if (neighbours == 3) array_output[i][j] = 1;
                    else if (array_input[i][j] == 1 && (neighbours == 2 || neighbours == 3)) array_output[i][j] = 1;
                    else array_output[i][j] = 0;
                }
            });

            //Hidden borders cleanup
            Parallel.For(0, x, i => array_output[i][0] = 0);
            Parallel.For(0, x, i => array_output[i][y - 1] = 0);
            Parallel.For(0, y, i => array_output[0][i] = 0);
            Parallel.For(0, y, i => array_output[x - 1][i] = 0);
        }

        /// <summary>
        /// Function that creates a Bitmap object, fills it with map data and displays it on screen using PictureBox control
        /// </summary>
        void draw()
        {
            //Fast drawing using unsafe code and parallel loops
            unsafe
            {
                BitmapData data = bitmap.LockBits(b_rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                //Pointer to the beginning of bitmap data
                byte* ptr = (byte*)data.Scan0;

                //Using map_0
                if (map_index == 0)
                {
                    Parallel.For(0, data.Height, y =>
                    {
                        byte* line = ptr + (y * data.Stride);

                        for (int x = 0; x < data.Width; x++)
                        {
                            line[x] = map_0[x + 1][y + 1] == 1 ? (byte)0 : (byte)255;
                        }
                    });
                }
                //Using map_1
                else
                {
                    Parallel.For(0, data.Height, y =>
                    {
                        byte* line = ptr + (y * data.Stride);

                        for (int x = 0; x < data.Width; x++)
                        {
                            line[x] = map_1[x + 1][y + 1] == 1 ? (byte)0 : (byte)255;
                        }
                    });
                }
                bitmap.UnlockBits(data);
            }

            //Draw bitmap
            if (resize_to_fit) draw_resized();
            else picGrid.Image = bitmap;            
        }

        /// <summary>
        /// Function that calculates the biggest possible size of bitmap to fit inside the window, redraws map on the larger bitmap and displays it on screen
        /// </summary>
        void draw_resized()
        {
            //Calculate scale
            int scale_x = picGrid.Width / bitmap.Width;
            int scale_y = picGrid.Height / bitmap.Height;
            int scale = scale_x < scale_y ? scale_x : scale_y;
            //If the window is smaller than the bitmap, set scale to 1
            if (scale == 0) scale = 1;

            //Calculate new size
            int width = bitmap.Width * scale;
            int height = bitmap.Height * scale;

            //If resize is needed, recreate bitmap_resized object with correct parameters
            if (bitmap_resized.Width != width || bitmap_resized.Height != height)
                bitmap_resized = new Bitmap(bitmap.Width * scale, bitmap.Height * scale, PixelFormat.Format24bppRgb);

            //Redraw map on the larger bitmap
            using (Graphics g = Graphics.FromImage(bitmap_resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap_resized.Width, bitmap_resized.Height));
            }

            //Display bitmap on the screen
            picGrid.Image = bitmap_resized;
        }

        private void loadFromImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Let user choose a file
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PNG files (*.png)|*.png";

            //If the file selection was successful
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //Read the file
                    Bitmap tmp = (Bitmap)Bitmap.FromFile(ofd.FileName);
                    //Read the grid dimensions
                    x = tmp.Width;
                    y = tmp.Height;

                    //Add one cell wide hidden border
                    x += 2;
                    y += 2;

                    //Initialize byte arrays
                    map_0 = new byte[x][];
                    map_1 = new byte[x][];
                    for (int i = 0; i < x; i++)
                    {
                        map_0[i] = new byte[y];
                        map_1[i] = new byte[y];
                    }

                    //Iterate through pixels
                    //If it's dark - it's a living cell
                    //If it's not dark - it's a dead cell
                    for (int i_x = 0; i_x < x - 2; i_x++)
                    {
                        for (int i_y = 0; i_y < y - 2; i_y++)
                        {
                            if (tmp.GetPixel(i_x, i_y).GetBrightness() < 0.1)
                                map_0[i_x + 1][i_y + 1] = 1;
                            else map_0[i_x + 1][i_y + 1] = 0;
                        }
                    }

                    //Initializing global variables
                    bitmap = new Bitmap(x - 2, y - 2, PixelFormat.Format8bppIndexed);
                    bitmap_resized = new Bitmap(x - 2, y - 2);
                    b_rectangle = new Rectangle(0, 0, x - 2, y - 2);
                    map_index = 0;

                    //Draw grid on the screen
                    draw();
                }
                //Error handling
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (x > 0 && y > 0)
                {
                    startToolStripMenuItem.Enabled = true;
                    nextStepToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void tmrStep_Tick(object sender, EventArgs e)
        {
            //Calculate next step using correct map
            if (map_index == 0)
            {
                calculate(map_0, map_1, x, y);
            }
            else
            {
                calculate(map_1, map_0, x, y);
            }
            
            //Switch correct map
            map_index = map_index == 0 ? 1 : 0;

            //Draw the calculated map
            draw();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmrStep.Start();
            startToolStripMenuItem.Enabled = false;
            nextStepToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmrStep.Stop();
            startToolStripMenuItem.Enabled = true;
            nextStepToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
        }

        private void nextStepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tmrStep_Tick(sender, e);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings settings = new Settings(this);
            settings.ShowDialog();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            //Load configuration

            //Check if configuration file exists
            if (File.Exists(SETTINGS_FILE))
            {
                load_settings();
            }
            //File doesn't exist, it needs to be created
            else
            {
                save_settings();
            }
        }

        /// <summary>
        /// Function handling loading settings from file
        /// </summary>
        public void load_settings()
        {
            try
            {
                using (StreamReader sr = new StreamReader(SETTINGS_FILE))
                {
                    int data = tmrStep.Interval;
                    int.TryParse(sr.ReadLine(), out data);
                    tmrStep.Interval = data;

                    data = 1;
                    int.TryParse(sr.ReadLine(), out data);
                    if (data == 1) resize_to_fit = true;
                    else resize_to_fit = false;
                }
            }
            //Error handling
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Function handling saving settings to file
        /// </summary>
        public void save_settings()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(SETTINGS_FILE, false))
                {
                    sw.WriteLine(tmrStep.Interval);
                    sw.WriteLine(resize_to_fit ? 1 : 0);
                }
            }
            //Error handling
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (bitmap != null) draw();
        }

        /// <summary>
        /// Keyboard handling
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool handled = false;

            switch (keyData)
            {
                case Keys.Control | Keys.O:
                    if (loadFromImageToolStripMenuItem.Enabled)
                    {
                        loadFromImageToolStripMenuItem_Click(this, null);
                        handled = true;
                    }
                    break;
                case Keys.F5:
                    if (startToolStripMenuItem.Enabled)
                    {
                        startToolStripMenuItem_Click(this, null);
                        handled = true;
                    }
                    break;
                case Keys.F6:
                    if (stopToolStripMenuItem.Enabled)
                    {
                        stopToolStripMenuItem_Click(this, null);
                        handled = true;
                    }
                    break;
                case Keys.F7:
                    if (nextStepToolStripMenuItem.Enabled)
                    {
                        nextStepToolStripMenuItem_Click(this, null);
                        handled = true;
                    }
                    break;
                case Keys.Control | Keys.S:
                    if (settingsToolStripMenuItem.Enabled)
                    {
                        settingsToolStripMenuItem_Click(this, null);
                        handled = true;
                    }
                    break;
                default:
                    handled = base.ProcessCmdKey(ref msg, keyData);
                    break;
            }

            return handled;
        }
    }
}
