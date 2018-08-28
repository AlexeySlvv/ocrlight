using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace OcrLight
{
    public partial class MainForm : Form
    {

        /// <summary>
        /// Properties.Settings.Default
        /// </summary>
        Properties.Settings Settings => Properties.Settings.Default;

        public MainForm()
        {
            InitializeComponent();

            Size = Settings.MainWindowSize;
            richTextBox.Font = Settings.RichTextBoxFont;
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            Text = Program.Name;            
        }

        private void MainFormSizeChanged(object sender, EventArgs e)
        {
            Settings.MainWindowSize = Size;
            Settings.Save();
        }

        #region Main menu

        private void FromPictureClick(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pictureBox.Image = Image.FromFile(openFileDialog.FileName);
                richTextBox.Text = string.Empty;
            }
        }

        private void FromClipboardClick(object sender, EventArgs e)
        {
            var data = Clipboard.GetDataObject();
            if (data.GetDataPresent(DataFormats.Bitmap))
            {
                pictureBox.Image = data.GetData(DataFormats.Bitmap) as Image;
                richTextBox.Text = string.Empty;
            }
            else
            {
                MessageBox.Show("Not image");
            }
        }

        private void SaveTextAsClick(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filename = saveFileDialog.FileName;
                using (var writer = new StreamWriter(filename))
                {
                    writer.Write(richTextBox.Text);
                }
            }
        }

        private void SelectFontClick(object sender, EventArgs e)
        {
            fontDialog.Font = Settings.RichTextBoxFont;
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                Settings.RichTextBoxFont = fontDialog.Font;
                Settings.Save();

                richTextBox.Font = Settings.RichTextBoxFont;
            }
        }

        private void CloseClick(object sender, EventArgs e)
        {
            Close();
        }

        #endregion Main menu

        #region Recognize

        /// <summary>
        /// Converts image to bitmap
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private Bitmap ImageToBitmap(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException();
            }

            var bitmap = new Bitmap(image.Width, image.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        /// <summary>
        /// Text recognizing
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private Task<string> GetOcrTextTaskAsync(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException();
            }

            return Task.Factory.StartNew(delegate
            {
                // https://github.com/tesseract-ocr/tessdata
                using (var engine = new TesseractEngine(@"./tessdata", "jpn"))
                {
                    var pix = PixConverter.ToPix(bitmap);
                    using (var page = engine.Process(pix))
                    {
                        return page.GetText().Trim();
                    }
                }
            });
        }

        /// <summary>
        /// Recognize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void buttonRecognizeClickAsync(object sender, EventArgs e)
        {
            var cursor = richTextBox.Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                richTextBox.Cursor = Cursors.WaitCursor;
                richTextBox.ReadOnly = true;

                var bitmap = ImageToBitmap(pictureBox.Image);
                richTextBox.Text = await GetOcrTextTaskAsync(bitmap);
            }
            finally
            {
                Cursor = Cursors.Default;
                richTextBox.Cursor = cursor;
                richTextBox.ReadOnly = false;
            }
        }

        #endregion Recognize

    }
}
