namespace GlimmerOfHope.Editor.Dialogue
{
    public static class DialogueCSVFormat
    {
        #region Column Indices

        public const int COL_LINE_ID = 0;
        public const int COL_CONV_ID = 1;
        public const int COL_ORDER = 2;
        public const int COL_SPEAKER = 3;
        public const int COL_EMOTION = 4;
        public const int COL_TEXT_FR = 5;
        public const int COL_TEXT_EN = 6;
        public const int COL_TEXT_ES = 7;
        public const int COL_NEXT_LINE = 8;

        public const int COL_CHOICE1_FR = 9;
        public const int COL_CHOICE1_EN = 10;
        public const int COL_CHOICE1_TARGET = 11;
        public const int COL_CHOICE1_FLAG = 12;

        public const int COL_CHOICE2_FR = 13;
        public const int COL_CHOICE2_EN = 14;
        public const int COL_CHOICE2_TARGET = 15;
        public const int COL_CHOICE2_FLAG = 16;

        public const int COL_CHOICE3_FR = 17;
        public const int COL_CHOICE3_EN = 18;
        public const int COL_CHOICE3_TARGET = 19;
        public const int COL_CHOICE3_FLAG = 20;

        public const int COL_CHOICE4_FR = 21;
        public const int COL_CHOICE4_EN = 22;
        public const int COL_CHOICE4_TARGET = 23;
        public const int COL_CHOICE4_FLAG = 24;

        #endregion

        #region Limits

        public const int MIN_COLUMNS = 9;
        public const int MAX_CHOICES = 4;
        public const int COLUMNS_PER_CHOICE = 4;

        #endregion

        #region Languages

        public static readonly string[] LANGUAGES = { "fr", "en", "es" };
        public const string DEFAULT_LANGUAGE = "fr";

        #endregion

        #region Asset Paths

        public const string LINES_FOLDER = "Assets/_Project/Data/Dialogue/Lines";
        public const string CONVERSATIONS_FOLDER = "Assets/_Project/Data/Dialogue/Conversations";
        public const string CHARACTERS_FOLDER = "Assets/_Project/Data/Dialogue/Characters";
        public const string LOCALIZATION_FOLDER = "Assets/StreamingAssets/Localization";

        #endregion

        #region Helper Methods

        public static int GetChoiceColumnOffset(int choiceIndex)
        {
            return COL_CHOICE1_FR + (choiceIndex * COLUMNS_PER_CHOICE);
        }

        public static string GetLocalizationPath(string language, string conversationId)
        {
            return $"{LOCALIZATION_FOLDER}/{language}/dialogue_{conversationId}.json";
        }

        #endregion
    }
}
