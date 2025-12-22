using System.Collections.Generic;

namespace TypeMagic.Models
{
    // Model representing a single parameter field in the UI
    public class FieldDefinition
    {
        #region Properties
        // Имя параметра в семействе Revit
        public string ParamName { get; set; }

        // Отображаемое имя в UI
        public string Label { get; set; }

        // Тип поля UI (Integer, Double, String, ElementId, CheckBox)
        public FieldType UiType { get; set; }

        // Список допустимых значений для выпадающего списка
        public List<string> Options { get; set; }

        // Минимальное значение (для Integer, Double)
        public double? Min { get; set; }

        // Максимальное значение (для Integer, Double)
        public double? Max { get; set; }

        // Префикс для фильтрации типов элементов (для ElementId)
        public string Prefix { get; set; }

        // Комментарий к параметру
        public string Comment { get; set; }

        //Группа
        public string Group { get; internal set; }
        #endregion

        #region Constructor
        // Конструктор по умолчанию
        public FieldDefinition()
        {
            Options = new List<string>();
        }
        #endregion
    }

    // Enum для типов полей UI
    public enum FieldType
    {
        Integer,
        Double,
        String,
        ElementId,
        CheckBox
    }
}
