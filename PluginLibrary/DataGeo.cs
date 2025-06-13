using PluginInterface;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Device.Location;
using System.Globalization;
using System.Threading;
using System.Diagnostics;

namespace PluginLibrary
{
    public class AddTimestampPlugin : IPlugin
    {
        public string Name => "Добавить дату и геолокацию";
        public string Author => "Банчан";

        public void Transform(Bitmap bitmap, CancellationToken token, IProgress<int> progress)
        {
            try
            {
                // Первый прогресс - начало работы
                progress?.Report(10);

                // Получаем текущую дату (быстрая операция)
                string dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                progress?.Report(30);

                // Получаем геолокацию (может быть долгой операцией)
                string locationText = GetLocationCoordinates(token);
                progress?.Report(60);

                if (locationText == null)
                {
                    locationText = "Геолокация недоступна";
                }

                // Формируем текст водяного знака
                string watermarkText = $"{dateText}\n{locationText}";
                progress?.Report(80);

                // Рисуем водяной знак на изображении
                DrawWatermark(bitmap, watermarkText, token);
                progress?.Report(100);
            }
            catch (OperationCanceledException)
            {
                throw; // Перебрасываем исключение отмены
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при добавлении водяного знака: {ex.Message}");
                throw; // Перебрасываем исключение дальше
            }
        }

        private void DrawWatermark(Bitmap bitmap, string text, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using (var graphics = Graphics.FromImage(bitmap))
            using (var font = new Font("Arial", 14, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var textBrush = new SolidBrush(Color.White))
            using (var outlineBrush = new SolidBrush(Color.Black))
            {
                // Рассчитываем размер и позицию
                SizeF textSize = graphics.MeasureString(text, font);
                float x = bitmap.Width - textSize.Width - 15;
                float y = bitmap.Height - textSize.Height - 15;

                // Рисуем контур (5 вариантов со смещением)
                for (int i = 0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    float offsetX = i switch
                    {
                        0 => -1,
                        1 => 1,
                        2 => -1,
                        3 => 1,
                        _ => 0
                    };
                    float offsetY = i switch
                    {
                        0 => -1,
                        1 => -1,
                        2 => 1,
                        3 => 1,
                        _ => 0
                    };
                    graphics.DrawString(text, font, outlineBrush, x + offsetX, y + offsetY);
                }

                // Рисуем основной текст
                token.ThrowIfCancellationRequested();
                graphics.DrawString(text, font, textBrush, x, y);
            }
        }

        private string GetLocationCoordinates(CancellationToken token)
        {
            try
            {
                using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High))
                {
                    var completionSource = new TaskCompletionSource<GeoPosition<GeoCoordinate>>();
                    var registration = token.Register(() => completionSource.TrySetCanceled());

                    watcher.PositionChanged += (sender, args) =>
                    {
                        if (!args.Position.Location.IsUnknown)
                        {
                            completionSource.TrySetResult(args.Position);
                        }
                    };

                    watcher.Start();

                    // Ожидаем с таймаутом и поддержкой отмены
                    var completedTask = Task.WhenAny(
                        completionSource.Task,
                        Task.Delay(3000, token)
                    ).Result;

                    if (completedTask == completionSource.Task)
                    {
                        var position = completionSource.Task.Result;
                        return $"{position.Location.Latitude:F2}° {position.Location.Longitude:F2}°";
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}