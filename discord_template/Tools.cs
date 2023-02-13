namespace discord_template
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
