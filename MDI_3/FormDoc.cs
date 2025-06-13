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
using System.Drawing.Drawing2D;
using System.IO;


namespace MDI_3
{
    public partial class FormDoc : Form
    {
        int startX, startY; // начальные координаты
        public Bitmap bitmap; // основной Bitmap
        Bitmap tempBitmap; // временный Bitmap
        private string filePath = null;
        private bool isDrawing = false;
        public bool isModified = false;

        public FormDoc()
        {
            InitializeComponent();
            bitmap = new Bitmap(800, 800);
            var g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            tempBitmap = new Bitmap(800, 800);

        }

        private void FormDoc_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = true;
                startX = e.X;
                startY = e.Y;
            }
        }

        private void FormDoc_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && e.Button == MouseButtons.Left)
            {
                switch(Form1.ToolNow)
                {
                    case Tools.Brush:
                    {
                        var g = Graphics.FromImage(bitmap);
                        int brushSize = Form1.WidthNow;
                        SolidBrush brush = new SolidBrush(Form1.ColorNow);
                        Pen pen = new Pen(brush, brushSize);
                        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                        g.DrawLine(pen, startX, startY, e.X, e.Y);
                        
                        startX = e.X;
                        startY = e.Y;
                        isModified = true;
                        Invalidate();
                        break;
                    }

                    case Tools.Line:
                        {
                            // очищаем временный Bitmap
                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                g.Clear(Color.Transparent);
                            }

                            // рисуем временную линию на временном Bitmap
                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                g.DrawLine(new Pen(Form1.ColorNow, Form1.WidthNow), startX, startY, e.X, e.Y);
                            }

                            isModified = true;
                            Invalidate();
                            break;
                        }

                    case Tools.Ellipse:
                        {
                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                g.Clear(Color.Transparent);
                            }

                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                int width = Math.Abs(e.X - startX);
                                int height = Math.Abs(e.Y - startY);
                                int x = Math.Min(startX, e.X);
                                int y = Math.Min(startY, e.Y);

                                g.DrawEllipse(new Pen(Form1.ColorNow, Form1.WidthNow), x, y, width, height);
                            }

                            isModified = true;
                            Invalidate();
                            break;
                        }
                    case Tools.FilledEllipse:
                        {
                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                g.Clear(Color.Transparent);
                            }

                            using (var g = Graphics.FromImage(tempBitmap))
                            {
                                int width = Math.Abs(e.X - startX);
                                int height = Math.Abs(e.Y - startY);
                                int x = Math.Min(startX, e.X);
                                int y = Math.Min(startY, e.Y);

                                g.FillEllipse(new SolidBrush(Form1.ColorNow), x, y, width, height);
                            }

                            isModified = true;
                            Invalidate();
                            break;
                        }
                    case Tools.Eraser:
                        {
                            var g = Graphics.FromImage(bitmap);
                            int brushSize = Form1.WidthNow;
                            SolidBrush brush = new SolidBrush(Color.White);
                            Pen pen = new Pen(brush, brushSize);
                            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            g.DrawLine(pen, startX, startY, e.X, e.Y);

                            startX = e.X;
                            startY = e.Y;
                            isModified = true;
                            Invalidate();
                            break;
                        }
                }
                
            }
        }

        private void FormDoc_MouseUp(object sender, MouseEventArgs e)
        {
            switch (Form1.ToolNow)
            {
                case Tools.Line:
                    {
                        // переносим временную линию на основной Bitmap
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.DrawLine(new Pen(Form1.ColorNow, Form1.WidthNow), startX, startY, e.X, e.Y);
                        }

                        // очищаем временный Bitmap
                        using (var g = Graphics.FromImage(tempBitmap))
                        {
                            g.Clear(Color.Transparent);
                        }

                        isModified = true;
                        Invalidate();
                        break;
                    }

                case Tools.Ellipse:
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            int width = Math.Abs(e.X - startX);
                            int height = Math.Abs(e.Y - startY);
                            int x = Math.Min(startX, e.X);
                            int y = Math.Min(startY, e.Y);

                            g.DrawEllipse(new Pen(Form1.ColorNow, Form1.WidthNow), x, y, width, height);
                        }

                        using (var g = Graphics.FromImage(tempBitmap))
                        {
                            g.Clear(Color.Transparent);
                        }

                        isModified = true;
                        Invalidate();
                        break;
                    }
                case Tools.FilledEllipse:
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            int width = Math.Abs(e.X - startX);
                            int height = Math.Abs(e.Y - startY);
                            int x = Math.Min(startX, e.X);
                            int y = Math.Min(startY, e.Y);

                            g.FillEllipse(new SolidBrush(Form1.ColorNow), x, y, width, height);
                        }

                        using (var g = Graphics.FromImage(tempBitmap))
                        {
                            g.Clear(Color.Transparent);
                        }

                        isModified = true;
                        Invalidate();
                        break;
                    }
            }
            
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // рисуем основной Bitmap
            e.Graphics.DrawImage(bitmap, 0, 0);

            // рисуем временный Bitmap (временную линию)
            if (isDrawing)
            {
                e.Graphics.DrawImage(tempBitmap, 0, 0);
            }
        }

        public void SaveAsImage()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "BMP Image|*.bmp|JPEG Image|*.jpg";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    SaveBitmap(filePath);
                }
                else
                    return;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isModified)
            {
                DialogResult result = MessageBox.Show(
                    $"Сохранить изменения в \"{this.Text}\" перед закрытием?",
                    "Документ изменен",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                switch (result)
                {
                    case DialogResult.Yes:
                        SaveAsImage();
                        if (string.IsNullOrEmpty(filePath))
                            e.Cancel = true; // если файл не был сохранён, отменяем закрытие формы
                        break;

                    case DialogResult.No:
                        // закрыть без сохранения
                        break;

                    case DialogResult.Cancel:
                        e.Cancel = true; // отменить закрытие
                        break;
                }

            }

            base.OnFormClosing(e);
        }

        public void SaveImage()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                SaveAsImage();
            }
            else
            {
                SaveBitmap(filePath);

            }
        }

        private void SaveBitmap(string path)
        {
            ImageFormat format = path.EndsWith(".jpg") ? ImageFormat.Jpeg : ImageFormat.Bmp;
            bitmap.Save(path, format);
        }

        public void LoadImage(string filePath)
        {
            try
            {
                var stream = new MemoryStream(File.ReadAllBytes(filePath));
                bitmap = new Bitmap(stream);
                Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки изображения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormDocument_MouseEnter(object sender, EventArgs e)
        {
            if (Form1.ToolNow == Tools.Brush ||
                Form1.ToolNow == Tools.Line ||
                Form1.ToolNow == Tools.Ellipse ||
                Form1.ToolNow == Tools.FilledEllipse)

                Cursor = Cursors.Cross;
            else if (Form1.ToolNow == Tools.Eraser)
                Cursor = Cursors.Hand;
            else
                Cursor = Cursors.Default;
        }


        private void FormDoc_MdiChildActivate(object sender, EventArgs e)
        {

        }

        private void FormDoc_Load(object sender, EventArgs e)
        {

        }
    }
}
