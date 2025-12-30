using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MagicEntry.Core.Interfaces;
using MagicEntry.Core.Models;
using System;
using System.IO;
using System.Linq;
using TypeMagic.Constants;
using TypeMagic.Services;
using TypeMagic.UI;

namespace TypeMagic.Commands
{
    // Command for renaming family types based on Excel configuration formulas
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RenameTypeCommand : IExternalCommand, IPlugin
    {
        #region IPlugin Implementation
        public PluginInfo Info { get; set; }
        public bool IsEnabled { get; set; }

        public bool Initialize()
        {
            return true;
        }

        public void Shutdown()
        {
        }
        #endregion

        #region IExternalCommand Implementation
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Получаем выбранный тип семейства
                var familySymbol = GetSelectedFamilySymbol(uiDoc);

                if (familySymbol == null)
                {
                    try
                    {
                        var reference = uiDoc.Selection.PickObject(
                            ObjectType.Element,
                            new FamilyInstanceSelectionFilter(),
                            Messages.InfoSelectElement);

                        var element = doc.GetElement(reference);
                        if (element is FamilyInstance familyInstance)
                        {
                            familySymbol = familyInstance.Symbol;
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }

                if (familySymbol == null)
                {
                    TaskDialog.Show(Messages.TitleError, Messages.ErrorNoSelection);
                    return Result.Cancelled;
                }

                string startTypeName = familySymbol.Name;
                // Находим файл конфигурации
                var configService = new FamilyConfigService();
                var familyName = familySymbol.FamilyName;
                var version = configService.GetFamilyVersion(familySymbol);

                if (string.IsNullOrWhiteSpace(version))
                {
                    TaskDialog.Show(Messages.TitleError,
                        string.Format(Messages.ErrorNoVersionParameter, AppConstants.VersionParameterName));
                    return Result.Failed;
                }

                var configFolder = configService.FindConfigFolder(familyName, version);
                if (configFolder == null)
                {
                    TaskDialog.Show(Messages.TitleError,
                        string.Format(Messages.ErrorConfigNotFound, version));
                    return Result.Failed;
                }

                var excelFile = Directory.GetFiles(configFolder, "*" + AppConstants.ExcelExtension).FirstOrDefault();
                if (excelFile == null)
                {
                    TaskDialog.Show(Messages.TitleError,
                        string.Format(Messages.ErrorConfigNotFound, version));
                    return Result.Failed;
                }

                // Генерируем имя типа
                var renameService = new TypeRenameService();
                var generatedName = renameService.GenerateTypeName(familySymbol, excelFile);

                if (string.IsNullOrEmpty(generatedName))
                {
                    TaskDialog.Show(Messages.TitleError, Messages.ErrorGenerateTypeName);
                    return Result.Failed;
                }

                // Показываем диалог для редактирования имени с валидацией
                bool nameIsValid = false;
                string finalName = generatedName;

                while (!nameIsValid)
                {
                    var dialog = new InputDialog(Messages.LabelTypeNamePrompt)
                    {
                        InputText = finalName
                    };

                    if (dialog.ShowDialog() != true)
                    {
                        return Result.Cancelled;
                    }

                    finalName = dialog.InputText;

                    var validation = renameService.ValidateTypeName(finalName, familySymbol);
                    if (validation.isValid)
                    {
                        nameIsValid = true;
                    }
                    else
                    {
                        TaskDialog.Show(Messages.TitleWarning, validation.errorMessage);
                    }
                }

                // Переименовываем тип
                renameService.RenameType(familySymbol, finalName, doc);
                TaskDialog.Show(Messages.TitleSuccess,
                    string.Format(Messages.InfoTypeRenamed, startTypeName, finalName));

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(Messages.TitleError,
                    string.Format(Messages.ErrorPluginExecution, ex.Message));
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        private FamilySymbol GetSelectedFamilySymbol(UIDocument uiDoc)
        {
            var doc = uiDoc.Document;
            var selection = uiDoc.Selection;
            var selectedIds = selection.GetElementIds();

            if (selectedIds.Count == 0)
                return null;

            var element = doc.GetElement(selectedIds.First());

            if (element is FamilyInstance familyInstance)
            {
                return familyInstance.Symbol;
            }

            if (element is FamilySymbol familySymbol)
            {
                return familySymbol;
            }

            return null;
        }
        #endregion
    }
}
