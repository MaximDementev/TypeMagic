using System.Collections.Generic;

namespace TypeMagic.Models
{
    // Model representing a group of parameters with an image
    public class GroupDefinition
    {
        #region Properties
        // Название группы
        public string Name { get; set; }

        // Путь к изображению группы
        public string ImagePath { get; set; }

        // Список полей параметров в группе
        public List<FieldDefinition> Fields { get; set; }
        #endregion

        #region Constructor
        // Конструктор по умолчанию
        public GroupDefinition()
        {
            Fields = new List<FieldDefinition>();
        }
        #endregion
    }
}
