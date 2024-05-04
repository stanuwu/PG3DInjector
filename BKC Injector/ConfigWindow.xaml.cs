using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace BKC_Injector;

public partial class ConfigWindow : Window
{
    private string _configPath;


    public ConfigWindow()
    {
        InitializeComponent();
        LoadConfig();
    }

    public static string GetConfigPath()
    {
        const string defaultConfigFile = "default";
        string steamInstallPath = GetSteamInstallPath();

        if (steamInstallPath != null)
        {
            string gameDir = Path.Combine(steamInstallPath, @"steamapps\common\Pixel Gun 3D PC Edition");
            string configDir = Path.Combine(gameDir, "bkc_config");
            Directory.CreateDirectory(configDir);

            string[] configFiles = Directory.GetFiles(configDir, "*.bkc");

            if (configFiles.Length > 0)
            {
                foreach (string configFile in configFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(configFile);
                    if (fileName.Equals(defaultConfigFile)) return configFile;
                }

                return configFiles[0];
            }

            return Path.Combine(configDir, defaultConfigFile + ".bkc");
        }

        return null; // Steam not found, handle error or default behavior
    }

    private static string GetSteamInstallPath()
    {
        string steamKey = "HKEY_CURRENT_USER\\Software\\Valve\\Steam";
        string steamPathValue = "SteamPath";
        object steamPath = Registry.GetValue(steamKey, steamPathValue, null);

        return steamPath?.ToString();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void LoadConfig()
    {
        var stackPanel = FindName("MainStackPanel") as StackPanel;

        if (stackPanel == null) throw new Exception("MainStackPanel not found in the XAML.");

        try
        {
            var lines = File.ReadAllLines(GetConfigPath());

            foreach (var line in lines)
            {
                var parts = line.Split(';');
                var key = parts[0];
                var type = parts[1];
                var value = parts.Length > 2 ? parts[2] : "";

                if (type == "enabled")
                {
                    var checkbox = new CheckBox
                    {
                        Content = $"{key} Enabled",
                        IsChecked = value == "1",
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Colors.White),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    stackPanel.Children.Add(checkbox);
                }
                else if (type == "slider" || type == "int_slider")
                {
                    var min = type == "slider" ? 0.0 : -10000;
                    var max = type == "slider" ? 10.0 : 10000;

                    var slider = new Slider
                    {
                        Minimum = min,
                        Maximum = max,
                        Width = 200,
                        Value = double.Parse(value),
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    var textBlock = new TextBlock
                    {
                        Text = $"{key} {type}:",
                        Margin = new Thickness(0, 0, 5, 0),
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Colors.LightGray)
                    };
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };
                    panel.Children.Add(textBlock);
                    panel.Children.Add(slider);
                    stackPanel.Children.Add(panel);
                }
                else if (type == "key")
                {
                    var textBox = new TextBox
                    {
                        Width = 200,
                        Text = value,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    var textBlock = new TextBlock
                    {
                        Text = $"{key} Key:",
                        Margin = new Thickness(0, 0, 5, 0),
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Colors.LightGray)
                    };
                    var panel = new StackPanel { Orientation = Orientation.Horizontal };
                    panel.Children.Add(textBlock);
                    panel.Children.Add(textBox);
                    stackPanel.Children.Add(panel);
                }

                stackPanel.Children.Add(new Separator
                {
                    Background = new SolidColorBrush(Colors.Gray), Height = 1, Margin = new Thickness(0, 10, 0, 10)
                });
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        // Prompt the user to select a configuration file
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "BKC Configuration Files (*.bkc)|*.bkc|All Files (*.*)|*.*";
        openFileDialog.InitialDirectory = GetDefaultConfigDirectory();

        if (openFileDialog.ShowDialog() == true)
        {
            string selectedConfigPath = openFileDialog.FileName;

            // Replace the default configuration with the selected one
            string defaultConfigPath = GetDefaultConfigPath();
            File.Copy(selectedConfigPath, defaultConfigPath, true);

            // Reload the configuration
            LoadConfig();

            MessageBox.Show("Configuration loaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private string GetDefaultConfigDirectory()
    {
        // Use the same directory where the executable is located as the initial directory
        return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    }

    private string GetDefaultConfigPath()
    {
        // Construct the path to the default configuration file
        return Path.Combine(GetDefaultConfigDirectory(), "default.bkc");
    }
    private void SaveConfig()
    {
        var stackPanel = FindName("MainStackPanel") as StackPanel;

        if (stackPanel == null)
        {
            MessageBox.Show("MainStackPanel not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var configLines = new List<string>();

        foreach (var child in stackPanel.Children)
            if (child is StackPanel panel && panel.Children.Count >= 2)
            {
                var keyBlock = panel.Children[0] as TextBlock;
                var control = panel.Children[1];

                var configLine = keyBlock.Text.Split(':')[0];

                if (control is Slider slider)
                    configLine += $";slider;{slider.Value}";
                else if (control is TextBox textBox) configLine += $";key;{textBox.Text}";

                configLines.Add(configLine);
            }
            else if (child is CheckBox checkbox)
            {
                var isChecked = checkbox.IsChecked.GetValueOrDefault(false) ? "1" : "0";
                var key = checkbox.Content.ToString().Replace(" Enabled", "");
                configLines.Add($"{key};enabled;{isChecked}");
            }

        try
        {
            File.WriteAllLines(GetConfigPath(), configLines);
            MessageBox.Show("Configurations saved successfully!", "Success", MessageBoxButton.OK);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}