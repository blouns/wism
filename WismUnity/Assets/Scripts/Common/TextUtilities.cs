namespace Assets.Scripts.Common
{
    public static class TextUtilities
    {
        public static string GetPresentVerb(string name)
        {
            return name.EndsWith("s") || name.EndsWith("ie") ? "are" : "is";
        }

        public static string GetPastVerb(string name)
        {
            return name.EndsWith("s") || name.EndsWith("ie") ? "have" : "has";
        }
    }
}
