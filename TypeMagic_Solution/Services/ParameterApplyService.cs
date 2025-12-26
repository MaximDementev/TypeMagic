using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using TypeMagic.Constants;
using TypeMagic.Models;

namespace TypeMagic.Services
{
    // Service for applying parameter values to family types
    public class ParameterApplyService
    {
        #region Public Methods
        // Применяет значения параметров к типу семейства
        public void ApplyParameters(FamilySymbol familySymbol, Dictionary<string, object> parameterValues, Document doc)
        {
            foreach (var kvp in parameterValues)
            {
                var paramName = kvp.Key;
                var value = kvp.Value;

                // Ищем параметр в типе семейства
                var param = familySymbol.LookupParameter(paramName);

                // Если параметр не найден в типе, пробуем найти в Family
                if (param == null && familySymbol.Family != null)
                {
                    param = familySymbol.Family.LookupParameter(paramName);
                }

                if (param == null || param.IsReadOnly)
                    continue;

                SetParameterValue(param, value);
            }
        }

        // Создает новый тип семейства на основе существующего
        public FamilySymbol CreateNewType(FamilySymbol baseFamilySymbol, string newTypeName, Document doc)
        {
            var newSymbol = baseFamilySymbol.Duplicate(newTypeName) as FamilySymbol;
            return newSymbol;
        }

        // Валидирует значения параметров перед применением
        public List<string> ValidateParameters(FormDefinition formDef, Dictionary<string, object> values)
        {
            var errors = new List<string>();

            foreach (var group in formDef.Groups)
            {
                foreach (var field in group.Fields)
                {
                    if (!values.ContainsKey(field.ParamName))
                        continue;

                    var value = values[field.ParamName];
                    var fieldErrors = ValidateField(field, value);
                    errors.AddRange(fieldErrors);
                }
            }

            return errors;
        }
        #endregion

        #region Private Methods
        // Устанавливает значение параметра в зависимости от типа
        private void SetParameterValue(Parameter param, object value)
        {
            if (value == null && param.StorageType != StorageType.String)
                return;
            string paramName = param.Definition.Name;
            ParameterType paramType = param.Definition.ParameterType;

            switch (param.StorageType)
            {
                case StorageType.Integer:
                    if (value is int intVal)
                        param.Set(intVal);
                    else if (value is bool boolVal)
                        param.Set(boolVal ? 1 : 0);
                    break;

                case StorageType.Double:

                    if (value is double doubleVal)
                    {
                        if (paramType == ParameterType.BarDiameter || paramType == ParameterType.ReinforcementLength)
                            doubleVal = UnitUtils.ConvertToInternalUnits(doubleVal, UnitTypeId.Millimeters);

                        param.Set(doubleVal);
                    }
                    break;

                case StorageType.String:
                    var strVal = value?.ToString() ?? string.Empty;
                    param.Set(strVal);
                    break;

                case StorageType.ElementId:
                    if (value is ElementId elemId)
                        param.Set(elemId);
                    break;
            }
        }

        // Валидирует одно поле
        private List<string> ValidateField(FieldDefinition field, object value)
        {
            var errors = new List<string>();

            if (value == null || (field.UiType != FieldType.String && field.UiType != FieldType.CheckBox && value.ToString() == string.Empty))
            {
                errors.Add(string.Format(Messages.ValidationValueRequired, field.Label));
                return errors;
            }

            switch (field.UiType)
            {
                case FieldType.Integer:
                    errors.AddRange(ValidateNumeric(field, value, true));
                    break;

                case FieldType.Double:
                    errors.AddRange(ValidateNumeric(field, value, false));
                    break;

                case FieldType.String:
                    errors.AddRange(ValidateString(field, value));
                    break;

                case FieldType.ElementId:
                    errors.AddRange(ValidateElementId(field, value));
                    break;
            }

            return errors;
        }

        // Валидация числовых значений
        private List<string> ValidateNumeric(FieldDefinition field, object value, bool isInteger)
        {
            var errors = new List<string>();

            double numValue;
            if (isInteger && value is int intVal)
                numValue = intVal;
            else if (!isInteger && value is double doubleVal)
                numValue = doubleVal;
            else
            {
                errors.Add(string.Format(Messages.ValidationInvalidNumber, field.Label));
                return errors;
            }

            double minValue = field.Min ?? double.MinValue;
            double maxValue = field.Max ?? double.MaxValue;

            // Проверка диапазона
            if (field.Min.HasValue && numValue < minValue)
                errors.Add(string.Format(Messages.ValidationValueRange, field.Label, minValue, maxValue == double.MaxValue ? "∞" : maxValue.ToString()));

            if (field.Max.HasValue && numValue > maxValue)
                errors.Add(string.Format(Messages.ValidationValueRange, field.Label, minValue == double.MinValue ? "-∞" : minValue.ToString(), maxValue));

            // Проверка списка допустимых значений
            if (field.Options != null && field.Options.Any())
            {
                var strValue = value.ToString();
                if (!field.Options.Contains(strValue))
                    errors.Add(string.Format(Messages.ValidationNotInList, field.Label));
            }

            return errors;
        }

        // Валидация строковых значений
        private List<string> ValidateString(FieldDefinition field, object value)
        {
            var errors = new List<string>();

            if (!(value is string strValue))
                return errors;

            // Проверка списка допустимых значений
            if (field.Options != null && field.Options.Any())
            {
                if (!field.Options.Contains(strValue))
                    errors.Add(string.Format(Messages.ValidationNotInList, field.Label));
            }

            return errors;
        }

        // Валидация ElementId
        private List<string> ValidateElementId(FieldDefinition field, object value)
        {
            var errors = new List<string>();

            if (!(value is ElementId elemId) || elemId == ElementId.InvalidElementId)
            {
                errors.Add(string.Format(Messages.ValidationInvalidElement, field.Label));
            }

            return errors;
        }
        #endregion
    }
}
