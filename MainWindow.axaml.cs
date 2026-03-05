using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace CryptoLab;

public partial class MainWindow : Window
{
    // ─── Цвета статуса ───────────────────────────────────────────
    private static readonly IBrush BrushOk  = new SolidColorBrush(Color.Parse("#27AE60"));
    private static readonly IBrush BrushErr = new SolidColorBrush(Color.Parse("#E74C3C"));

    public MainWindow()
    {
        InitializeComponent();
    }

    // ══════════════════════════════════════════════════════════════
    // СТОЛБЦОВЫЙ МЕТОД — обработчики
    // ══════════════════════════════════════════════════════════════

    private void ColEncrypt_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateColumn(out var txt, out var k1, out var k2)) return;
        Try(() =>
        {
            var filtered = CipherAlgorithms.FilterRussian(txt);
            if (filtered.Length == 0)
            {
                ColOutputBox.Text = string.Empty;
                SetStatus(ColStatusLabel, false, "⚠ В тексте нет букв русского алфавита — нечего шифровать.");
                return;
            }
            string result = CipherAlgorithms.DoubleColumnEncrypt(txt, k1, k2);
            ColOutputBox.Text = result;
            SetStatus(ColStatusLabel, ok: true,
                $"✓ Зашифровано — входных букв: {filtered.Length}, выходных: {result.Length}");
        }, ColStatusLabel);
    }

    private void ColDecrypt_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateColumn(out var txt, out var k1, out var k2)) return;
        Try(() =>
        {
            var filtered = CipherAlgorithms.FilterRussian(txt);
            if (filtered.Length == 0)
            {
                ColOutputBox.Text = string.Empty;
                SetStatus(ColStatusLabel, false, "⚠ В тексте нет букв русского алфавита — нечего расшифровывать.");
                return;
            }
            string result = CipherAlgorithms.DoubleColumnDecrypt(txt, k1, k2);
            ColOutputBox.Text = result;
            SetStatus(ColStatusLabel, ok: true,
                $"✓ Расшифровано — входных букв: {filtered.Length}, выходных: {result.Length}");
        }, ColStatusLabel);
    }

    private void ColClear_Click(object? sender, RoutedEventArgs e)
    {
        ColInputBox.Text = ColOutputBox.Text = string.Empty;
        ColKey1Box.Text  = ColKey2Box.Text  = string.Empty;
        ColStatusLabel.Text = string.Empty;
    }

    private async void ColLoadFile_Click(object? sender, RoutedEventArgs e)
        => await LoadFile(ColInputBox, ColStatusLabel);

    private async void ColSaveInput_Click(object? sender, RoutedEventArgs e)
        => await SaveFile(ColInputBox.Text, ColStatusLabel);

    private async void ColSaveOutput_Click(object? sender, RoutedEventArgs e)
        => await SaveFile(ColOutputBox.Text, ColStatusLabel);

    private async void ColCopy_Click(object? sender, RoutedEventArgs e)
        => await CopyToClipboard(ColOutputBox.Text, ColStatusLabel);

    private void ColSwap_Click(object? sender, RoutedEventArgs e)
    {
        ColInputBox.Text = ColOutputBox.Text;
        ColOutputBox.Text = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════
    // ШИФР ВИЖЕНЕРА — обработчики
    // ══════════════════════════════════════════════════════════════

    private void VigEncrypt_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateVigenere(out var txt, out var key)) return;
        Try(() =>
        {
            var filtered = CipherAlgorithms.FilterRussian(txt);
            if (filtered.Length == 0)
            {
                VigOutputBox.Text = string.Empty;
                SetStatus(VigStatusLabel, false, "⚠ В тексте нет букв русского алфавита — нечего шифровать.");
                return;
            }
            string result = CipherAlgorithms.VigenereAutoEncrypt(txt, key);
            VigOutputBox.Text = result;
            SetStatus(VigStatusLabel, ok: true, $"✓ Зашифровано — букв: {filtered.Length}");
        }, VigStatusLabel);
    }

    private void VigDecrypt_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateVigenere(out var txt, out var key)) return;
        Try(() =>
        {
            var filtered = CipherAlgorithms.FilterRussian(txt);
            if (filtered.Length == 0)
            {
                VigOutputBox.Text = string.Empty;
                SetStatus(VigStatusLabel, false, "⚠ В тексте нет букв русского алфавита — нечего расшифровывать.");
                return;
            }
            string result = CipherAlgorithms.VigenereAutoDecrypt(txt, key);
            VigOutputBox.Text = result;
            SetStatus(VigStatusLabel, ok: true, $"✓ Расшифровано — букв: {filtered.Length}");
        }, VigStatusLabel);
    }

    private void VigClear_Click(object? sender, RoutedEventArgs e)
    {
        VigInputBox.Text = VigOutputBox.Text = string.Empty;
        VigKeyBox.Text   = string.Empty;
        VigStatusLabel.Text = string.Empty;
    }

    private async void VigLoadFile_Click(object? sender, RoutedEventArgs e)
        => await LoadFile(VigInputBox, VigStatusLabel);

    private async void VigSaveInput_Click(object? sender, RoutedEventArgs e)
        => await SaveFile(VigInputBox.Text, VigStatusLabel);

    private async void VigSaveOutput_Click(object? sender, RoutedEventArgs e)
        => await SaveFile(VigOutputBox.Text, VigStatusLabel);

    private async void VigCopy_Click(object? sender, RoutedEventArgs e)
        => await CopyToClipboard(VigOutputBox.Text, VigStatusLabel);

    private void VigSwap_Click(object? sender, RoutedEventArgs e)
    {
        VigInputBox.Text = VigOutputBox.Text;
        VigOutputBox.Text = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════
    // Валидация
    // ══════════════════════════════════════════════════════════════

    private bool ValidateColumn(out string txt, out string k1, out string k2)
    {
        txt = ColInputBox.Text ?? string.Empty;
        k1  = ColKey1Box.Text  ?? string.Empty;
        k2  = ColKey2Box.Text  ?? string.Empty;

        if (string.IsNullOrWhiteSpace(txt))
        { SetStatus(ColStatusLabel, false, "✗ Введите исходный текст."); return false; }

        if (string.IsNullOrWhiteSpace(k1))
        { SetStatus(ColStatusLabel, false, "✗ Введите ключевое слово 1."); return false; }

        if (string.IsNullOrWhiteSpace(k2))
        { SetStatus(ColStatusLabel, false, "✗ Введите ключевое слово 2."); return false; }

        if (CipherAlgorithms.FilterRussian(k1).Length == 0)
        { SetStatus(ColStatusLabel, false, "✗ Ключевое слово 1 не содержит русских букв."); return false; }

        if (CipherAlgorithms.FilterRussian(k2).Length == 0)
        { SetStatus(ColStatusLabel, false, "✗ Ключевое слово 2 не содержит русских букв."); return false; }

        // Текст может содержать что угодно — русские буквы извлечём внутри алгоритма
        // Если русских букв в тексте нет — не ошибка, просто вернём пустой результат
        return true;
    }

    private bool ValidateVigenere(out string txt, out string key)
    {
        txt = VigInputBox.Text ?? string.Empty;
        key = VigKeyBox.Text   ?? string.Empty;

        if (string.IsNullOrWhiteSpace(txt))
        { SetStatus(VigStatusLabel, false, "✗ Введите исходный текст."); return false; }

        if (string.IsNullOrWhiteSpace(key))
        { SetStatus(VigStatusLabel, false, "✗ Введите ключевое слово."); return false; }

        if (CipherAlgorithms.FilterRussian(key).Length == 0)
        { SetStatus(VigStatusLabel, false, "✗ Ключевое слово не содержит русских букв."); return false; }

        // Текст может содержать что угодно — русские буквы извлечём внутри алгоритма
        return true;
    }

    // ══════════════════════════════════════════════════════════════
    // Файловые операции
    // ══════════════════════════════════════════════════════════════

    private async Task LoadFile(TextBox target, TextBlock statusLabel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title          = "Открыть текстовый файл",
                AllowMultiple  = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Все файлы")       { Patterns = new[] { "*.*"  } }
                }
            });

            if (files.Count > 0)
            {
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                target.Text = await reader.ReadToEndAsync();
                SetStatus(statusLabel, ok: true, $"✓ Загружен: {files[0].Name}");
            }
        }
        catch (Exception ex)
        {
            SetStatus(statusLabel, ok: false, $"✗ Ошибка чтения: {ex.Message}");
        }
    }

    private async Task SaveFile(string? content, TextBlock statusLabel)
    {
        if (string.IsNullOrEmpty(content))
        {
            SetStatus(statusLabel, ok: false, "✗ Нет текста для сохранения.");
            return;
        }
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title           = "Сохранить файл",
                SuggestedFileName = "result",
                DefaultExtension  = "txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Текстовые файлы") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("Все файлы")       { Patterns = new[] { "*.*"  } }
                }
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(content);
                SetStatus(statusLabel, ok: true, $"✓ Сохранён: {file.Name}");
            }
        }
        catch (Exception ex)
        {
            SetStatus(statusLabel, ok: false, $"✗ Ошибка сохранения: {ex.Message}");
        }
    }

    private async Task CopyToClipboard(string? content, TextBlock statusLabel)
    {
        if (string.IsNullOrEmpty(content))
        {
            SetStatus(statusLabel, ok: false, "✗ Нет текста для копирования.");
            return;
        }
        try
        {
            var clipboard = Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(content);
                SetStatus(statusLabel, ok: true, "✓ Скопировано в буфер обмена.");
            }
        }
        catch (Exception ex)
        {
            SetStatus(statusLabel, ok: false, $"✗ Ошибка копирования: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    // Вспомогательные
    // ══════════════════════════════════════════════════════════════

    private static void SetStatus(TextBlock label, bool ok, string message)
    {
        label.Text       = message;
        label.Foreground = ok ? BrushOk : BrushErr;
    }

    private void Try(Action action, TextBlock statusLabel)
    {
        try { action(); }
        catch (Exception ex) { SetStatus(statusLabel, ok: false, $"✗ Ошибка: {ex.Message}"); }
    }
}
