using Autodesk.Revit.DB;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TypeMagic.Constants;

namespace TypeMagic.Services
{
    // Service for renaming family types based on Excel configuration formulas
    public class TypeRenameService
    {
        #region Fields
        private readonly FamilyNameGenerator _nameGenerator;
        private static readonly char[] InvalidChars = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '{', '}', '[', ']' };
        #endregion

        #region Constructor
        public TypeRenameService()
        {
            _nameGenerator = new FamilyNameGenerator();
        }
        #endregion

        #region Public Methods
        // Генерирует имя типа на основе формул из Excel
        public string GenerateTypeName(FamilySymbol familySymbol, string excelPath)
        {
            var formulas = LoadTypeNameFormulas(excelPath);
            if (formulas == null || formulas.Count == 0)
                return null;

            PopulateParameters(familySymbol);

            var nameParts = new List<string>();

            foreach (var formula in formulas)
            {
                if (string.IsNullOrWhiteSpace(formula))
                    continue;

                var result = _nameGenerator.Generate(formula);
                if (!string.IsNullOrEmpty(result) && result != "false")
                {
                    nameParts.Add(result);
                }
            }

            return string.Join("", nameParts);
        }

        // Проверяет валидность имени типа
        public (bool isValid, string errorMessage) ValidateTypeName(string typeName, FamilySymbol currentSymbol)
        {
            // Проверка на пустое имя
            if (string.IsNullOrWhiteSpace(typeName))
                return (false, "Имя типа не может быть пустым.");

            // Проверка длины
            if (typeName.Length > 128)
                return (false, $"Имя типа слишком длинное ({typeName.Length} символов). Максимум 128 символов.");

            // Проверка недопустимых символов
            var invalidCharsFound = typeName.Where(c => InvalidChars.Contains(c)).ToList();
            if (invalidCharsFound.Any())
                return (false, $"Имя типа содержит недопустимые символы: {string.Join(", ", invalidCharsFound.Distinct())}");

            // Проверка на дублирование имени типа в том же семействе
            var family = currentSymbol.Family;
            var existingTypeIds = family.GetFamilySymbolIds();

            foreach (ElementId typeId in existingTypeIds)
            {
                var existingSymbol = family.Document.GetElement(typeId) as FamilySymbol;
                if (existingSymbol != null &&
                    existingSymbol.Id != currentSymbol.Id &&
                    existingSymbol.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"Тип с именем '{typeName}' уже существует в этом семействе.");
                }
            }

            return (true, null);
        }

        // Переименовывает тип семейства
        public void RenameType(FamilySymbol familySymbol, string newName, Document doc)
        {
            using (Transaction trans = new Transaction(doc, "Переименовать тип"))
            {
                trans.Start();
                familySymbol.Name = newName;
                trans.Commit();
            }
        }
        #endregion

        #region Private Methods
        // Загружает формулы из столбца TypeName в Excel
        private List<string> LoadTypeNameFormulas(string excelPath)
        {
            if (!File.Exists(excelPath))
                return null;

            var formulas = new List<string>();

            try
            {
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
                            return null;

                        var dataTable = dataSet.Tables[0];

                        if (!dataTable.Columns.Contains(AppConstants.ColTypeName))
                            return null;

                        foreach (DataRow row in dataTable.Rows)
                        {
                            var formula = row[AppConstants.ColTypeName]?.ToString()?.Trim();
                            formulas.Add(formula);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return formulas;
        }

        // Заполняет параметры типа для генератора имен
        private void PopulateParameters(FamilySymbol familySymbol)
        {
            _nameGenerator.Parameters.Clear();
            _nameGenerator.Bools.Clear();

            foreach (Parameter param in familySymbol.Parameters)
            {
                if (param.HasValue)
                {
                    var paramName = param.Definition.Name;

                    if (param.StorageType == StorageType.Integer)
                    {
                        ForgeTypeId dataType = param.Definition.GetDataType();
                        if (dataType == SpecTypeId.Boolean.YesNo)
                        {
                            _nameGenerator.Bools[paramName] = param.AsInteger() == 1;
                        }
                        else
                        {
                            _nameGenerator.Parameters[paramName] = param.AsInteger();
                        }
                    }
                    else if (param.StorageType == StorageType.Double)
                    {
                        ForgeTypeId dataType = param.Definition.GetDataType();
                        double d = param.AsDouble();


                        if ( dataType == SpecTypeId.BarDiameter || dataType == SpecTypeId.Length || dataType == SpecTypeId.ReinforcementLength)
                        {
                            // Конвертация в мм                            
                            d = UnitUtils.ConvertFromInternalUnits(d, UnitTypeId.Millimeters);
                        }

                        _nameGenerator.Parameters[paramName] = d;
                    }
                    else if (param.StorageType == StorageType.String)
                    {
                        _nameGenerator.Parameters[paramName] = param.AsString();
                    }
                    else if (param.StorageType == StorageType.ElementId)
                    {
                        var elemId = param.AsElementId();
                        if (elemId != null && elemId != ElementId.InvalidElementId)
                        {
                            var element = familySymbol.Document.GetElement(elemId);
                            _nameGenerator.Parameters[paramName] = element?.Name ?? "";
                        }
                    }
                }
            }
        }
        #endregion
    }
}
