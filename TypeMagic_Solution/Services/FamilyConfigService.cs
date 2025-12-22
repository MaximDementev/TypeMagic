using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using TypeMagic.Constants;
using TypeMagic.Models;

namespace TypeMagic.Services
{
    // Service for managing family configuration paths and loading
    public class FamilyConfigService
    {
        #region Fields
        private readonly ExcelConfigService _excelService;
        #endregion

        #region Constructor
        // Конструктор с зависимостью от ExcelConfigService
        public FamilyConfigService()
        {
            _excelService = new ExcelConfigService();
        }
        #endregion

        #region Public Methods
        // Получает версию семейства из параметра
        public string GetFamilyVersion(FamilySymbol familySymbol)
        {
            var param = familySymbol.LookupParameter(AppConstants.VersionParameterName);
            if (param == null || !param.HasValue)
                return null;

            return param.AsString();
        }

        // Находит папку конфигурации для семейства и версии
        public string FindConfigFolder(string familyName, string version)
        {
            // Здесь должна быть логика поиска корневой папки семейства
            // Для примера используем заглушку
            var baseFolder = @"C:\Users\Dementev\Desktop\Revit\Families\" + familyName;
            var versionFolder = Path.Combine(baseFolder, version);

            if (Directory.Exists(versionFolder))
                return versionFolder;

            return null;
        }

        // Загружает конфигурацию для семейства
        public FormDefinition LoadFamilyConfig(FamilySymbol familySymbol)
        {
            var familyName = familySymbol.FamilyName;
            var version = GetFamilyVersion(familySymbol);

            if (string.IsNullOrWhiteSpace(version))
                throw new Exception(string.Format(Messages.ErrorNoVersionParameter, AppConstants.VersionParameterName));

            var configFolder = FindConfigFolder(familyName, version);
            if (configFolder == null)
                throw new Exception(string.Format(Messages.ErrorConfigNotFound, version));

            var excelFile = Directory.GetFiles(configFolder, "*" + AppConstants.ExcelExtension).FirstOrDefault();
            if (excelFile == null)
                throw new Exception(string.Format(Messages.ErrorConfigNotFound, version));

            return _excelService.LoadConfiguration(excelFile, configFolder, familyName, version);
        }
        #endregion
    }
}
