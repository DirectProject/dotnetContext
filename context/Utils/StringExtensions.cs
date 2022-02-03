namespace Health.Direct.Context.Utils
{
    public static class StringExtensions
    {
        public static string FormatContentId(this string contentId)
        {
            if (contentId.StartsWith("<") && contentId.EndsWith(">"))
            {
                return contentId;
            }

            return $"<{contentId}>";
        }
    }
}
