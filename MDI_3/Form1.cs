using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PluginInterface;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading;

namespace MDI_3
{
    public enum Tools
    {
        Brush,
        Line,
        Ellipse,
        FilledEllipse,
        Eraser
    }

    public partial class Form1 : Form
    {
        public static Color ColorNow { get; set; }
        public static int WidthNow { get; set; }
        public static Tools ToolNow;
        Dictionary<string, IPlugin> plugins = new Dictionary<string, IPlugin>();

        private CancellationTokenSource cts;
        private Progress<int> progress;

        public class PluginConfig
        {
            public Dictionary<string, bool> PluginsAccess { get; set; } = new Dictionary<string, bool>();
        }

        private const string ConfigFileName = "plugins.json";
        private PluginConfig config;

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string json = File.ReadAllText(ConfigFileName);
                    config = JsonSerializer.Deserialize<PluginConfig>(json);
                }
                else
                {
                    // Создаем конфиг по умолчанию при первом запуске
                    config = new PluginConfig();
                    foreach (var plugin in plugins)
                    {
                        config.PluginsAccess[plugin.Value.Name] = true; // По умолчанию все плагины включены
                    }
                    SaveConfig();
                }
            }
            catch
            {
                // Если что-то пошло не так, создаем пустую конфигурацию
                config = new PluginConfig();
            }
        }

        private void SaveConfig()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigFileName, json);

        }

        private void LoadPlugins()
        {
            // Сначала находим все плагины
            FindPlugins();

            // Затем загружаем конфигурацию
            LoadConfig();

            // Создаем временный словарь для отфильтрованных плагинов
            var filteredPlugins = new Dictionary<string, IPlugin>();

            foreach (var plugin in plugins)
            {
                // Если плагина нет в конфиге, добавляем его с значением true
                if (!config.PluginsAccess.ContainsKey(plugin.Key))
                {
                    config.PluginsAccess[plugin.Key] = true;
                }

                // Добавляем только включенные плагины
                if (config.PluginsAccess[plugin.Key])
                {
                    filteredPlugins.Add(plugin.Key, plugin.Value);
                }
            }

            // Заменяем исходный словарь отфильтрованным
            plugins = filteredPlugins;

            SaveConfig();
        }

        public Form1()
        {
            InitializeComponent();
            ColorNow = Color.Black;
            WidthNow = 5;
            ToolNow = Tools.Brush;
            FindPlugins();
            LoadPlugins();
            CreatePluginsMenu();
            UpdateMenuState();
        }

        void FindPlugins()
        {
            plugins = new Dictionary<string, IPlugin>(); // Инициализируем словарь

            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(folder, "*.dll");

            foreach (string file in files)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);
                    foreach (Type type in assembly.GetTypes())
                    {
                        Type iface = type.GetInterface("PluginInterface.IPlugin");
                        if (iface != null)
                        {
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                            if (!plugins.ContainsKey(plugin.Name)) // Проверка на дубликаты
                            {
                                plugins.Add(plugin.Name, plugin);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка загрузки плагина {file}: {ex.Message}");
                }
            }
        }

        private void CreatePluginsMenu()
        {
            фильтрыToolStripMenuItem.DropDownItems.Clear();

            foreach (var p in plugins)
            {
                // Проверяем, что плагин включен в конфигурации
                if (config.PluginsAccess.TryGetValue(p.Value.Name, out bool isEnabled) && isEnabled)
                {
                    var item = фильтрыToolStripMenuItem.DropDownItems.Add(p.Value.Name);
                    item.Click += OnPluginClick;
                    item.Tag = p.Value; // Сохраняем ссылку на плагин
                }
            }
        }
        private async void OnPluginClick(object sender, EventArgs e)
        {
            // Отменяем предыдущую операцию, если она была
            cts?.Cancel();
            cts = new CancellationTokenSource();
            progress = new Progress<int>(percent =>
            {
                progressBar1.Value = percent;
            });

            // Получаем активный документ
            var activeDocument = this.ActiveMdiChild as FormDoc;
            if (activeDocument?.bitmap == null) return;

            // Получаем выбранный плагин
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null || !plugins.TryGetValue(menuItem.Text, out IPlugin plugin))
            {
                MessageBox.Show("Плагин не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Блокируем интерфейс на время обработки
                Cursor.Current = Cursors.WaitCursor;
                progressBar1.Visible = true;
                buttonCancel.Visible = true;

                // Создаем копию изображения для безопасной работы в другом потоке
                using (var bitmapCopy = new Bitmap(activeDocument.bitmap))
                {
                    // Запускаем обработку в фоновом потоке
                    await Task.Run(() => plugin.Transform(bitmapCopy, cts.Token, progress));

                    // Возвращаемся в UI-поток для обновления
                    activeDocument.bitmap = new Bitmap(bitmapCopy);
                    activeDocument.Refresh();
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Операция отменена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке изображения: {ex.Message}", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Восстанавливаем интерфейс
                Cursor.Current = Cursors.Default;
                progressBar1.Visible = false;
                buttonCancel.Visible = false;
                activeDocument.isModified = true;
            }
        }


        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form_about = new FormAbout();
            form_about.ShowDialog();
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form childForm = new FormDoc();
            childForm.MdiParent = this;
            childForm.Text = "Документ";
            childForm.Show();
            UpdateMenuState();
        }

        private void RedButton_Click(object sender, EventArgs e)
        {
            ColorNow = Color.Red;
        }

        private void GreenButton_Click(object sender, EventArgs e)
        {
            ColorNow = Color.Green;
        }

        private void BlueButton_Click(object sender, EventArgs e)
        {
            ColorNow = Color.Blue;
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            var color_dlg = new ColorDialog();
            if (color_dlg.ShowDialog() == DialogResult.OK)
                ColorNow = color_dlg.Color;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            WidthNow = 1;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            WidthNow = 5;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            WidthNow = 10;
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            WidthNow = 15;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            WidthNow = 20;
        }

        private void UpdateMenuState()
        {
            bool hasActiveChild = this.ActiveMdiChild != null;
            сохранитьToolStripMenuItem.Enabled = hasActiveChild;
            сохранитьКакToolStripMenuItem.Enabled = hasActiveChild;
            окнаToolStripMenuItem.Enabled = hasActiveChild;
        }

        private void Form1_MdiChildActivate(object sender, EventArgs e)
        {
            UpdateMenuState();
        }

        private void помощьToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild is FormDoc doc)
            {
                doc.SaveAsImage();
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveMdiChild is FormDoc doc)
            {
                doc.SaveImage();
            }
        }

        private void окнаToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void каскадомToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.Cascade);
        }

        private void рядомToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.TileVertical);
        }

        private void упорядочитьЗначкиToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.bmp;*.jpg;*.jpeg)|*.bmp;*.jpg;*.jpeg|Все файлы (*.*)|*.*",
                Title = "Открыть изображение"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                FormDoc childForm = new FormDoc();
                childForm.MdiParent = this;
                childForm.Text = openFileDialog.FileName;
                childForm.LoadImage(openFileDialog.FileName);
                childForm.Show();
            }
        }

        private void drawLine_Click(object sender, EventArgs e)
        {
            ToolNow = Tools.Line;
        }

        private void drawEllipse_Click(object sender, EventArgs e)
        {
            ToolNow = Tools.Ellipse;
        }

        private void killEraser_Click(object sender, EventArgs e)
        {
            ToolNow = Tools.Eraser;
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            ToolNow = Tools.FilledEllipse;
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            ToolNow = Tools.Brush;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string pluginsInfo = GetPluginsInfo();
            MessageBox.Show(pluginsInfo, "Информация о плагинах",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public string GetPluginsInfo()
        {
            if (plugins == null || plugins.Count == 0)
                return "Плагины не загружены";

            var info = new StringBuilder();
            info.AppendLine("Загруженные плагины:");
            info.AppendLine("-------------------");

            int counter = 1;
            foreach (var plugin in plugins)
            {
                info.AppendLine($"{counter}. {plugin.Key}");
                info.AppendLine($"   Автор: {plugin.Value.Author}");
                info.AppendLine();
                counter++;
            }

            info.AppendLine($"Всего загружено: {plugins.Count} плагинов");
            return info.ToString();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
        }
    }
}
