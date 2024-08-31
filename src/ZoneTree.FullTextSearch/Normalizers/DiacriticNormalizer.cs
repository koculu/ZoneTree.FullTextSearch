using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace ZoneTree.FullTextSearch.Normalizers;

/// <summary>
/// The DiacriticNormalizer class provides functionality to normalize characters by removing diacritical marks
/// and converting them to their base forms. This class supports customization through a character map and an exclusion set.
/// </summary>
public sealed class DiacriticNormalizer : ICharNormalizer, IStringNormalizer
{
    /// <summary>
    /// The default mapping of characters with diacritics to their base characters.
    /// This dictionary includes common accented characters and their corresponding base characters.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, char> DefaultCharMap =
        new Dictionary<char, char>
        {
            // Uppercase mappings
            {'Â', 'A'}, {'À', 'A'}, {'Á', 'A'}, {'Ä', 'A'}, {'Ã', 'A'}, {'Å', 'A'},
            {'Ç', 'C'},
            {'É', 'E'}, {'È', 'E'}, {'Ê', 'E'}, {'Ë', 'E'},
            {'Í', 'I'}, {'Ì', 'I'}, {'Î', 'I'}, {'Ï', 'I'},
            {'Ñ', 'N'},
            {'Ó', 'O'}, {'Ò', 'O'}, {'Ô', 'O'}, {'Ö', 'O'}, {'Õ', 'O'},
            {'Ú', 'U'}, {'Ù', 'U'}, {'Û', 'U'}, {'Ü', 'U'},
            {'Ý', 'Y'},
            {'Ş', 'S'},

            // Lowercase mappings
            {'â', 'a'}, {'à', 'a'}, {'á', 'a'}, {'ä', 'a'}, {'ã', 'a'}, {'å', 'a'},
            {'ç', 'c'},
            {'é', 'e'}, {'è', 'e'}, {'ê', 'e'}, {'ë', 'e'},
            {'ı', 'i'},
            {'í', 'i'}, {'ì', 'i'}, {'î', 'i'}, {'ï', 'i'},
            {'ñ', 'n'},
            {'ó', 'o'}, {'ò', 'o'}, {'ô', 'o'}, {'ö', 'o'}, {'õ', 'o'},
            {'ú', 'u'}, {'ù', 'u'}, {'û', 'u'}, {'ü', 'u'},
            {'ý', 'y'}, {'ÿ', 'y'},
            {'ş', 's'}
        };

    /// <summary>
    /// The character map used for normalization, mapping characters with diacritics to their base characters.
    /// This can be customized through the constructor.
    /// </summary>
    readonly IReadOnlyDictionary<char, char> CharMap;

    /// <summary>
    /// A set of characters that should be excluded from normalization.
    /// If a character is in this set, it will be returned as-is, without normalization.
    /// </summary>
    readonly IReadOnlySet<char> ExcludeSet;

    /// <summary>
    /// Initializes a new instance of the DiacriticNormalizer class.
    /// Allows for customization of the character map and exclusion set.
    /// </summary>
    /// <param name="charMap">A custom character map for normalization. If null, the default map is used.</param>
    /// <param name="exclude">A set of characters to exclude from normalization. If null, an empty set is used.</param>
    public DiacriticNormalizer(
        IReadOnlyDictionary<char, char> charMap = null,
        IReadOnlySet<char> exclude = null)
    {
        CharMap = charMap ?? DefaultCharMap;
        ExcludeSet = exclude ?? new HashSet<char>();
    }

    /// <summary>
    /// Normalizes a single character by removing diacritical marks and converting it to its base form.
    /// If the character is in the exclusion set, it is returned as-is.
    /// </summary>
    /// <param name="input">The character to normalize.</param>
    /// <returns>The normalized character, or the original character if it is in the exclusion set or cannot be normalized.</returns>
    public char Normalize(char input)
    {
        if (ExcludeSet.Contains(input))
            return input;

        if (CharMap.TryGetValue(input, out char baseChar))
            return baseChar;

        var normalized = input.ToString().Normalize(NormalizationForm.FormD);
        var len = normalized.Length;
        for (var i = 0; i < len; i++)
        {
            char c = normalized[i];
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                return c;
        }
        return input;
    }

    /// <summary>
    /// Normalizes a string by removing diacritical marks from each character and converting them to their base forms.
    /// Characters in the exclusion set are returned as-is.
    /// </summary>
    /// <param name="input">The string to normalize, provided as a ReadOnlySpan of characters.</param>
    /// <returns>The normalized string.</returns>
    public string Normalize(ReadOnlySpan<char> input)
    {
        var len = input.Length;
        for (int i = 0; i < len; i++)
        {
            char currentChar = input[i];
            if (!ExcludeSet.Contains(currentChar) &&
                CharMap.ContainsKey(currentChar))
            {
                var sb = new StringBuilder(len);
                sb.Append(input.Slice(0, i));
                for (int j = i; j < len; j++)
                {
                    sb.Append(Normalize(input[j]));
                }
                return sb.ToString();
            }
        }
        return input.ToString();
    }
}
