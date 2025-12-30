using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data; // для вычисления выражений

namespace TypeMagic.Services
{
    // Service for generating family type names from formulas
    public class FamilyNameGenerator
    {
        #region Properties
        public Dictionary<string, object> Parameters { get; set; }
        public Dictionary<string, bool> Bools { get; set; }
        #endregion

        #region Constructor
        public FamilyNameGenerator()
        {
            Parameters = new Dictionary<string, object>();
            Bools = new Dictionary<string, bool>();
        }
        #endregion

        #region Public Methods
        // Генерирует имя типа из формулы
        public string Generate(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return string.Empty;

            // Обработка условий { }
            formula = Regex.Replace(formula, @"\{(.+?)\}", m =>
            {
                bool condition = EvaluateCondition(m.Groups[1].Value);
                return condition ? "true" : "false";
            });

            // Если условие false → весь блок можно удалить
            if (formula.Contains("false"))
                return null;

            // Подстановка параметров [Param|modifiers]
            string result = Regex.Replace(formula, @"\[(.+?)\]", m =>
            {
                string token = m.Groups[1].Value;
                string[] parts = token.Split('|');
                string paramName = parts[0];
                string[] modifiers = new string[0];
                if (parts.Length > 1)
                {
                    modifiers = new string[parts.Length - 1];
                    Array.Copy(parts, 1, modifiers, 0, modifiers.Length);
                }

                object val = Parameters.ContainsKey(paramName) ? Parameters[paramName] : null;
                string strVal = val != null ? val.ToString() : (Bools.ContainsKey(paramName) && Bools[paramName] ? "true" : "false");

                // Применяем модификаторы
                strVal = ApplyModifiers(strVal, modifiers);
                return strVal;
            });

            result = result.Replace("true→", "");

            return result;
        }
        #endregion

        #region Private Methods
        // Оценивает условное выражение {…}
        private bool EvaluateCondition(string cond)
        {
            cond = cond.Trim();

            // Подставляем true/false для параметров
            cond = Regex.Replace(cond, @"\[(.+?)\]", m =>
            {
                string key = m.Groups[1].Value;
                string val = "false";
                if (Bools.TryGetValue(key, out var b)) val = b ? "true" : "false";
                if (Parameters.TryGetValue(key, out var pVal)) val = pVal != null && pVal.ToString() != "0" ? "true" : "false";
                return val.ToLower();
            });

            // Заменяем OR/AND/NOT на C# логические операторы
            cond = cond.Replace("true", "1").Replace("false", "0")
           .Replace("AND", "*")   // логическое AND
           .Replace("OR", "+")    // логическое OR
           .Replace("NOT", "1-"); // логическое NOT

            // Вычисляем логическое выражение через DataTable
            try
            {
                DataTable dt = new DataTable();
                var result = dt.Compute(cond, "");
                return Convert.ToBoolean(result);
            }
            catch
            {
                return false;
            }
        }

        // Применяет модификаторы к значению параметра
        private string ApplyModifiers(string strValue, string[] modifiers)
        {
            if (string.IsNullOrEmpty(strValue)) return "";

            double d;
            bool isNumber = double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
            string s = strValue;

            foreach (var mod in modifiers)
            {
                if (mod.StartsWith("/"))
                {
                    if (isNumber)
                    {
                        double div = double.Parse(mod.Substring(1), CultureInfo.InvariantCulture);
                        d /= div;
                        s = d.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (mod.StartsWith("*"))
                {
                    if (isNumber)
                    {
                        double mul = double.Parse(mod.Substring(1), CultureInfo.InvariantCulture);
                        d *= mul;
                        s = d.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (mod == "round")
                {
                    if (isNumber)
                    {
                        d = Math.Round(d);
                        s = d.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (mod.StartsWith("round:"))
                {
                    if (isNumber)
                    {
                        int digits = int.Parse(mod.Split(':')[1]);
                        d = Math.Round(d, digits);
                        s = d.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (mod == "upper") s = s.ToUpper();
                else if (mod == "lower") s = s.ToLower();
            }

            return s;
        }
        #endregion
    }
}
