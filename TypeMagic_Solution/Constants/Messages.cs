namespace TypeMagic.Constants
{
    // Static class for storing all user-facing messages
    public static class Messages
    {
        #region Error Messages
        public const string ErrorNoSelection = "Пожалуйста, выберите экземпляр семейства или тип в диспетчере проекта.";
        public const string ErrorNoFamilySymbol = "Выбранный элемент не является семейством.";
        public const string ErrorNoVersionParameter = "Параметр версии '{0}' не найден в семействе.";
        public const string ErrorConfigNotFound = "Файл конфигурации не найден для версии '{0}'.";
        public const string ErrorExcelCorrupted = "Ошибка при чтении файла конфигурации Excel: {0}";
        public const string ErrorServicesUnavailable = "Сервисы MagicEntry недоступны.";
        public const string ErrorPluginExecution = "Ошибка при выполнении плагина: {0}";
        public const string ErrorValidation = "Ошибка валидации: {0}";
        public const string ErrorTransaction = "Ошибка при применении изменений: {0}";
        public const string ErrorCreateType = "Ошибка при создании нового типа: {0}";
        public const string ErrorApply = "Ошибка при применении параметров: {0}";
        #endregion

        #region Info Messages
        public const string InfoApplySuccess = "Параметры успешно применены.";
        public const string InfoNewTypeCreated = "Создан новый тип: {0}";
        public const string InfoPluginInitialized = "Плагин {0} инициализирован успешно.";
        public const string InfoSelectElement = "Выберите экземпляр семейства в модели";
        #endregion

        #region Validation Messages
        public const string ValidationValueRequired = "Поле '{0}' обязательно для заполнения.";
        public const string ValidationValueRange = "Значение '{0}' должно быть в диапазоне от {1} до {2}.";
        public const string ValidationInvalidNumber = "Некорректное числовое значение для поля '{0}'.";
        public const string ValidationInvalidElement = "Некорректный элемент для поля '{0}'.";
        public const string ValidationNotInList = "Значение '{0}' не входит в список допустимых значений.";
        #endregion

        #region UI Labels
        public const string LabelApply = "Применить";
        public const string LabelCreateNew = "Создать новый тип";
        public const string LabelCancel = "Отмена";
        public const string LabelNewTypeName = "Имя нового типа:";
        public const string WindowTitle = "Type Magic - Редактор параметров типа";
        #endregion

        #region Dialog Titles
        public const string TitleError = "Ошибка";
        public const string TitleSuccess = "Успешно";
        public const string TitleWarning = "Предупреждение";
        public const string TitleInfo = "Информация";
        #endregion
    }
}
