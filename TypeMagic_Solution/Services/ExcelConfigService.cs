using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TypeMagic.Constants;
using TypeMagic.Models;

namespace TypeMagic.Services
{
    // Service for loading configuration from Excel files
    public class ExcelConfigService
    {
        static ExcelConfigService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #region Public Methods
        // Загружает конфигурацию формы из Excel файла
        public FormDefinition LoadConfiguration(string excelPath, string configFolderPath, string familyName, string version)
        {
            if (!File.Exists(excelPath))
                throw new FileNotFoundException($"Excel файл не найден: {excelPath}");

            var formDefinition = new FormDefinition
            {
                FamilyKey = familyName,
                Version = version
            };

            using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    if (dataSet.Tables.Count == 0)
                        throw new Exception("Excel файл не содержит листов.");

                    var dataTable = dataSet.Tables[0];
                    var fields = ParseDataTable(dataTable);
                    var groups = GroupFieldsByGroup(fields, configFolderPath);
                    formDefinition.Groups = groups;
                }
            }

            return formDefinition;
        }
        #endregion

        #region Private Methods
        private List<FieldDefinition> ParseDataTable(DataTable dataTable)
        {
            var fields = new List<FieldDefinition>();

            foreach (DataRow row in dataTable.Rows)
            {
                var field = ParseRow(row, dataTable.Columns);
                if (field != null)
                    fields.Add(field);
            }

            return fields;
        }

        private FieldDefinition ParseRow(DataRow row, DataColumnCollection columns)
        {
            var paramName = GetCellValue(row, columns, AppConstants.ColParamName);
            if (string.IsNullOrWhiteSpace(paramName))
                return null;

            var field = new FieldDefinition
            {
                ParamName = paramName,
                Label = GetCellValue(row, columns, AppConstants.ColLabel) ?? paramName,
                Comment = GetCellValue(row, columns, AppConstants.ColComment) ?? string.Empty,
                Group = GetCellValue(row, columns, AppConstants.ColGroup) ?? "default"
            };



            // Парсинг типа
            var typeStr = GetCellValue(row, columns, AppConstants.ColType);
            field.UiType = ParseFieldType(typeStr);

            // Парсинг DropDownList
            var dropDownStr = GetCellValue(row, columns, AppConstants.ColDropDownList);
            if (!string.IsNullOrWhiteSpace(dropDownStr))
            {
                field.Options = dropDownStr.Split(',').Select(s => s.Trim()).ToList();
            }

            // Парсинг Min/Max
            var minStr = GetCellValue(row, columns, AppConstants.ColMin);
            var maxStr = GetCellValue(row, columns, AppConstants.ColMax);
            if (double.TryParse(minStr, out double min))
                field.Min = min;
            if (double.TryParse(maxStr, out double max))
                field.Max = max;

            // Парсинг Prefix
            field.Prefix = GetCellValue(row, columns, AppConstants.ColPrefix) ?? string.Empty;

            return field;
        }

        private string GetCellValue(DataRow row, DataColumnCollection columns, string columnName)
        {
            if (columns.Contains(columnName))
            {
                var value = row[columnName];
                return value != null && value != DBNull.Value ? value.ToString()?.Trim() : null;
            }
            return null;
        }

        // Парсит строку типа в enum FieldType
        private FieldType ParseFieldType(string typeStr)
        {
            if (string.IsNullOrWhiteSpace(typeStr))
                return FieldType.String;

            if (Enum.TryParse<FieldType>(typeStr, true, out var fieldType))
                return fieldType;

            return FieldType.String;
        }

        // Группирует поля по значению Group и добавляет пути к изображениям
        private List<GroupDefinition> GroupFieldsByGroup(List<FieldDefinition> fields, string configFolderPath)
        {
            var groupDict = new Dictionary<string, GroupDefinition>();

            foreach (var field in fields)
            {
                var groupName = field.Group; 

                if (!groupDict.ContainsKey(groupName))
                {
                    var imagePath = FindGroupImage(configFolderPath, groupName);
                    groupDict[groupName] = new GroupDefinition
                    {
                        Name = groupName,
                        ImagePath = imagePath
                    };
                }

                groupDict[groupName].Fields.Add(field);
            }

            return groupDict.Values.ToList();
        }

        // Ищет изображение для группы в папке конфигурации
        private string FindGroupImage(string configFolderPath, string groupName)
        {
            var extensions = new[] { AppConstants.ImageExtensionPng, AppConstants.ImageExtensionJpg, ".jpeg" };

            foreach (var ext in extensions)
            {
                var imagePath = Path.Combine(configFolderPath, groupName + ext);
                if (File.Exists(imagePath))
                    return imagePath;
            }

            return null;
        }
        #endregion
    }
}
