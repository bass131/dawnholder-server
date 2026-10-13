namespace Shared.GameData;

public static class AccountNameRule
{
    public const int MinLength = 3;
    public const int MaxLength = 16;

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (raw == null || raw.Length < MinLength || raw.Length > MaxLength) return false;

        char[] characters = raw.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (character >= 'A' && character <= 'Z') character = (char)(character + ('a' - 'A'));
            if (!((character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') || character == '_'))
            {
                return false;
            }
            characters[i] = character;
        }

        normalized = new string(characters);
        return true;
    }
}
