using System.Collections.Generic;

namespace TypeMagic.Models
{
    // Model representing the complete form configuration loaded from Excel
    public class FormDefinition
    {
        #region Properties
        // Ключ семейства (имя семейства)
        public string FamilyKey { get; set; }

        // Версия семейства
        public string Version { get; set; }

        // Список групп параметров
        public List<GroupDefinition> Groups { get; set; }
        #endregion

        #region Constructor
        // Конструктор по умолчанию
        public FormDefinition()
        {
            Groups = new List<GroupDefinition>();
        }
        #endregion
    }
}
