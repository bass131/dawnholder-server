namespace Dawnholder.Client.UI
{
    public static class MainMenuNotice
    {
        // 메뉴 오류 글자칸의 폰트(정적 atlas)에 있는 글자만 쓴다. 가운뎃점 U+00B7은 그 폰트에 없다.
        public const string InvalidAccountName = "계정 이름은 영문 소문자, 숫자, 밑줄 3~16자로 입력해주세요";
        public const string AlreadyOnlineAccount = "이미 접속 중인 계정입니다";
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
