using PluginInterface;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLibrary
{
    public class MedianFilterPlugin : IPlugin
    {
        public string Name => "Медианный фильтр (3x3)";
        public string Author => "Банчан";

        public void Transform(Bitmap bitmap, CancellationToken token, IProgress<int> progress)
        {
            // Создаем временную копию для исходных данных
            using (var sourceBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
            using (var resultBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
            {
                // Копируем исходное изображение
                using (var g = Graphics.FromImage(sourceBitmap))
                {
                    g.DrawImage(bitmap, 0, 0);
                }

                // Обрабатываем изображение
                ProcessMedianFilter(sourceBitmap, resultBitmap, token, progress);

                // Копируем результат обратно
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.DrawImage(resultBitmap, 0, 0);
                }
            }
        }

        private void ProcessMedianFilter(Bitmap source, Bitmap destination,
                                       CancellationToken token, IProgress<int> progress)
        {
            var rect = new Rectangle(0, 0, source.Width, source.Height);

            // Блокируем исходное изображение только для чтения
            BitmapData sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            // Блокируем результирующее изображение только для записи
            BitmapData destData = destination.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            try
            {
                int bytesPerPixel = 3;
                int stride = sourceData.Stride;
                int height = source.Height;
                int width = source.Width;
                int byteCount = stride * height;

                byte[] sourcePixels = new byte[byteCount];
                byte[] destPixels = new byte[byteCount];

                // Копируем исходные данные
                System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, sourcePixels, 0, byteCount);

                // Для плавного обновления прогресса
                int lastReportedProgress = -1;
                int progressUpdateInterval = Math.Max(height / 20, 1); // Обновлять каждые ~5%

                // Параллельная обработка по строкам с поддержкой отмены
                Parallel.For(1, height - 1, new ParallelOptions { CancellationToken = token }, y =>
                {
                    // Проверяем запрос на отмену
                    token.ThrowIfCancellationRequested();

                    for (int x = 1; x < width - 1; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;

                        // Буфер для значений окрестности 3x3 (9 пикселей × 3 канала)
                        byte[] neighborhood = new byte[27];
                        int pos = 0;

                        // Собираем значения из окрестности
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int kernelIndex = (y + ky) * stride + (x + kx) * bytesPerPixel;
                                neighborhood[pos++] = sourcePixels[kernelIndex];     // B
                                neighborhood[pos++] = sourcePixels[kernelIndex + 1]; // G
                                neighborhood[pos++] = sourcePixels[kernelIndex + 2]; // R
                            }
                        }

                        // Находим медиану для каждого канала
                        destPixels[index] = GetMedian(neighborhood, 0);     // B
                        destPixels[index + 1] = GetMedian(neighborhood, 1); // G
                        destPixels[index + 2] = GetMedian(neighborhood, 2); // R
                    }

                    // Плавное обновление прогресса
                    int currentProgress = (y * 100) / height;
                    if (currentProgress > lastReportedProgress ||
                        y % progressUpdateInterval == 0 ||
                        y == height - 2)
                    {
                        progress?.Report(currentProgress);
                        lastReportedProgress = currentProgress;
                    }
                });

                // Копируем результат обратно
                System.Runtime.InteropServices.Marshal.Copy(destPixels, 0, destData.Scan0, byteCount);
            }
            finally
            {
                source.UnlockBits(sourceData);
                destination.UnlockBits(destData);
            }
        }

        private byte GetMedian(byte[] neighborhood, int offset)
        {
            byte[] values = new byte[9];
            for (int i = 0; i < 9; i++)
            {
                values[i] = neighborhood[i * 3 + offset];
            }
            Array.Sort(values);
            return values[4];
        }
    }
}