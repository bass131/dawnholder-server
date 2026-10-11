namespace Dawnholder.Client.UI
{
    public static class MainMenuNotice
    {
        public const string InvalidAccountName = "계정 이름은 영문 소문자·숫자·밑줄 3~16자로 입력해주세요";
        static string s_pending;

        public static void Set(string message) => s_pending = message;

        public static bool TryTake(out string message)
        {
            message = s_pending;
            s_pending = null;
            return !string.IsNullOrEmpty(message);
        }
    }
}
