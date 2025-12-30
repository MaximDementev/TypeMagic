using Autodesk.Revit.DB;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TypeMagic.UI
{
    /// <summary>
    /// Конвертер для отображения типов элементов в формате "(FamilyName) TypeName"
    /// </summary>
    public class ElementTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ElementType elementType)
            {
                string familyName = elementType.FamilyName ?? string.Empty;
                string typeName = elementType.Name ?? string.Empty;
                
                if (!string.IsNullOrEmpty(familyName))
                {
                    return $"({familyName}) {typeName}";
                }
                
                return typeName;
            }

            if (value is Element element)
            {
                return element.Name ?? string.Empty;
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
