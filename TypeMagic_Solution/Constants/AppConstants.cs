namespace TypeMagic.Constants
{
    // Static class for storing application constants
    public static class AppConstants
    {
        #region Plugin Info
        public const string PluginName = "TypeMagic";
        public const string PluginDisplayName = "Type Magic";
        public const string VersionParameterName = "ADSK_Версия семейства";
        #endregion

        #region File Extensions
        public const string ExcelExtension = ".xlsx";
        public const string ImageExtensionPng = ".png";
        public const string ImageExtensionJpg = ".jpg";
        #endregion

        #region Excel Columns
        public const string ColParamName = "ParamName";
        public const string ColLabel = "Label";
        public const string ColGroup = "Group";
        public const string ColType = "Type";
        public const string ColDropDownList = "DropDownList";
        public const string ColMax = "Max";
        public const string ColMin = "Min";
        public const string ColPrefix = "Prefix";
        public const string ColComment = "Comment";
        #endregion

        #region UI Settings
        public const int WindowWidth = 1400;
        public const int WindowHeight = 900;
        public const int WindowMinWidth = 1200;
        public const int WindowMinHeight = 700;
        #endregion
    }
}
