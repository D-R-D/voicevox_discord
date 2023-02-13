namespace voicevox_discord
{
    public static class Tools
    {
        public static bool IsNullOrEmpty(this string? str)
        {
            if (str == null) { return true; }
            if (str == "") { return true; }
            return false;
        }
    }
}
