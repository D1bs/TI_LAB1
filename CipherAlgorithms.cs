using System;
using System.Linq;
using System.Text;

namespace CryptoLab;

/// <summary>
/// Алгоритмы шифрования по варианту 10:
///  1. Столбцовый метод с двумя ключевыми словами
///  2. Шифр Виженера с самогенерирующимся ключом
/// Работает только с буквами русского алфавита (33 буквы, включая ё).
/// </summary>
public static class CipherAlgorithms
{
    public const string Alphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
    private const int N = 33;

    // ─── Вспомогательные ──────────────────────────────────────────

    /// <summary>Оставляет только строчные буквы русского алфавита.</summary>
    public static string FilterRussian(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text.ToLower())
            if (Alphabet.Contains(c)) sb.Append(c);
        return sb.ToString();
    }

    private static int Idx(char c) => Alphabet.IndexOf(char.ToLower(c));

    /// <summary>
    /// Порядок считывания столбцов по ключевому слову.
    /// Буквы ключа нумеруются по алфавиту; при совпадении — по позиции слева направо.
    /// Возвращает: order[rank] = исходный номер столбца.
    /// </summary>
    private static int[] ColumnOrder(string key)
    {
        var pairs = key.Select((c, i) => (c, i))
                       .OrderBy(p => Idx(p.c))
                       .ThenBy(p => p.i)
                       .ToArray();
        int[] order = new int[key.Length];
        for (int r = 0; r < key.Length; r++)
            order[r] = pairs[r].i;
        return order;
    }

    // ─── Столбцовая перестановка (одна итерация) ──────────────────

    private static string ColEnc(string text, string key)
    {
        int cols = key.Length;
        int rows = (text.Length + cols - 1) / cols;
        // Сетка с нулевым заполнителем
        char[] grid = new char[rows * cols];
        for (int i = 0; i < grid.Length; i++)
            grid[i] = i < text.Length ? text[i] : '\0';

        int[] order = ColumnOrder(key);
        var sb = new StringBuilder(text.Length);
        foreach (int col in order)
            for (int row = 0; row < rows; row++)
            {
                char c = grid[row * cols + col];
                if (c != '\0') sb.Append(c);
            }
        return sb.ToString();
    }

    private static string ColDec(string cipher, string key)
    {
        int cols = key.Length;
        int rows = (cipher.Length + cols - 1) / cols;
        // Сколько столбцов «полные» (имеют rows символов)
        int fullCols = cipher.Length % cols == 0 ? cols : cipher.Length % cols;

        int[] order = ColumnOrder(key);

        // Длина каждого столбца (по исходному номеру)
        int[] lengths = new int[cols];
        for (int oc = 0; oc < cols; oc++)
            lengths[oc] = cipher.Length % cols == 0
                ? rows
                : (oc < fullCols ? rows : rows - 1);

        // Разбиваем шифротекст на столбцы (в порядке order)
        string[] columns = new string[cols];
        int pos = 0;
        foreach (int oc in order)
        {
            columns[oc] = cipher.Substring(pos, lengths[oc]);
            pos += lengths[oc];
        }

        // Читаем построчно
        int[] cp = new int[cols];
        var sb = new StringBuilder(cipher.Length);
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
                if (cp[col] < columns[col].Length)
                    sb.Append(columns[col][cp[col]++]);
        return sb.ToString();
    }

    // ─── Публичный API: двойной столбцовый ───────────────────────

    public static string DoubleColumnEncrypt(string plainText, string key1, string key2)
    {
        string t = FilterRussian(plainText);
        if (t.Length == 0) return string.Empty;
        string k1 = FilterRussian(key1);
        string k2 = FilterRussian(key2);
        return ColEnc(ColEnc(t, k1), k2);
    }

    public static string DoubleColumnDecrypt(string cipherText, string key1, string key2)
    {
        string t = FilterRussian(cipherText);
        if (t.Length == 0) return string.Empty;
        string k1 = FilterRussian(key1);
        string k2 = FilterRussian(key2);
        return ColDec(ColDec(t, k2), k1);   // обратный порядок!
    }

    // ─── Публичный API: Виженер самогенерирующийся ───────────────

    /// <summary>
    /// Шифрование Виженером с самогенерирующимся ключом.
    /// K = keyWord[0..m-1] + plainText[0..n-m-1]
    /// C[i] = (M[i] + K[i]) mod 33
    /// </summary>
    public static string VigenereAutoEncrypt(string plainText, string keyWord)
    {
        string text = FilterRussian(plainText);
        string key  = FilterRussian(keyWord);
        if (text.Length == 0) return string.Empty;
        if (key.Length == 0)
            throw new ArgumentException("Ключ не содержит букв русского алфавита.");

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            int m = Idx(text[i]);
            int k = i < key.Length ? Idx(key[i]) : Idx(text[i - key.Length]);
            sb.Append(Alphabet[(m + k) % N]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Дешифрование Виженером с самогенерирующимся ключом.
    /// M[i] = (C[i] - K[i] + 33) mod 33
    /// Ключ восстанавливается из уже расшифрованных символов.
    /// </summary>
    public static string VigenereAutoDecrypt(string cipherText, string keyWord)
    {
        string cipher = FilterRussian(cipherText);
        string key    = FilterRussian(keyWord);
        if (cipher.Length == 0) return string.Empty;
        if (key.Length == 0)
            throw new ArgumentException("Ключ не содержит букв русского алфавита.");

        char[] plain = new char[cipher.Length];
        for (int i = 0; i < cipher.Length; i++)
        {
            int c = Idx(cipher[i]);
            int k = i < key.Length ? Idx(key[i]) : Idx(plain[i - key.Length]);
            plain[i] = Alphabet[((c - k) % N + N) % N];
        }
        return new string(plain);
    }
}
