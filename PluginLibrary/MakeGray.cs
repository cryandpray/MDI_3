using PluginInterface;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLibrary
{
    public class GrayscaleTransform : IPlugin
    {
        public string Name => "Оттенки серого";
        public string Author => "Банчан";

        public void Transform(Bitmap bitmap, CancellationToken token, IProgress<int> progress)
        {
            // Создаем временную копию в правильном формате
            using (var tempBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
            {
                // Копируем исходное изображение с конвертацией формата
                using (var g = Graphics.FromImage(tempBitmap))
                {
                    g.DrawImage(bitmap, 0, 0);
                }

                // Обрабатываем временное изображение
                ProcessBitmap(tempBitmap, token, progress);

                // Копируем результат обратно в исходное изображение
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(tempBitmap, 0, 0);
                }
            }
        }

        private void ProcessBitmap(Bitmap bitmap, CancellationToken token, IProgress<int> progress)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = null;

            try
            {
                // Блокируем битмап в памяти
                bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                int bytesPerPixel = 3; // Для Format24bppRgb
                int stride = bmpData.Stride;
                int height = bitmap.Height;
                int width = bitmap.Width;
                int byteCount = stride * height;

                byte[] pixels = new byte[byteCount];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, byteCount);

                // Обновляем прогресс каждые 5% или каждые 10 строк (что наступит раньше)
                int progressUpdateThreshold = Math.Max(height / 20, 1);
                int lastReportedProgress = -1;

                // Оптимизированная параллельная обработка с поддержкой отмены
                Parallel.For(0, height, new ParallelOptions { CancellationToken = token }, y =>
                {
                    // Проверяем отмену операции
                    token.ThrowIfCancellationRequested();

                    //Task.Delay(1, token).Wait(token);

                    int currentLine = y * stride;
                    int pixelCountInRow = width;

                    for (int x = 0; x < pixelCountInRow; x++)
                    {
                        int index = currentLine + x * bytesPerPixel;

                        // Получаем компоненты цвета (BGR)
                        byte b = pixels[index];
                        byte g = pixels[index + 1];
                        byte r = pixels[index + 2];

                        // Формула преобразования в grayscale
                        byte gray = (byte)((r * 0.3) + (g * 0.59) + (b * 0.11));

                        // Записываем результат
                        pixels[index] = gray;
                        pixels[index + 1] = gray;
                        pixels[index + 2] = gray;
                    }

                    // Плавное обновление прогресса
                    int currentProgress = (y * 100) / height;
                    if (currentProgress > lastReportedProgress ||
                        y % progressUpdateThreshold == 0 ||
                        y == height - 1)
                    {
                        progress?.Report(currentProgress);
                        lastReportedProgress = currentProgress;
                    }
                });

                // Копируем данные обратно
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, byteCount);
            }
            finally
            {
                if (bmpData != null)
                    bitmap.UnlockBits(bmpData);
            }
        }
    }
}