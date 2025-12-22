using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using MagicEntry.Core.Interfaces;
using MagicEntry.Core.Models;
using MagicEntry.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using TypeMagic.Constants;
using TypeMagic.Services;
using TypeMagic.UI;

namespace TypeMagic.Commands
{
    // Main command for TypeMagic plugin integrated with MagicEntry system
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TypeMagicCommand : IExternalCommand, IPlugin
    {
        #region IPlugin Implementation
        public PluginInfo Info { get; set; }
        public bool IsEnabled { get; set; }

        // Выполняет внутреннюю инициализацию плагина при загрузке системы
        public bool Initialize()
        {
            try
            {
                var pathService = ServiceProvider.GetService<IPathService>();
                var initService = ServiceProvider.GetService<IPluginInitializationService>();

                if (pathService == null || initService == null)
                    return false;

                var pluginName = Info?.Name ?? AppConstants.PluginName;
                if (!initService.InitializePlugin(pluginName))
                    return false;

                var settingsPath = pathService.GetPluginUserDataFilePath(pluginName, "settings.txt");
                if (!File.Exists(settingsPath))
                {
                    File.WriteAllText(settingsPath,
                        string.Format(Messages.InfoPluginInitialized, AppConstants.PluginDisplayName) +
                        "\nInitialized: " + DateTime.Now);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Освобождает ресурсы, используемые плагином
        public void Shutdown()
        {
            // Cleanup logic if needed
        }
        #endregion

        #region IExternalCommand Implementation
        // Точка входа для выполнения команды плагина
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var doc = uiDoc.Document;

                // Получаем выбранный элемент (из Selection или из Project Browser)
                var familySymbol = GetSelectedFamilySymbol(uiDoc);

                // Если ничего не выбрано, предлагаем выбрать экземпляр
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

                // Загружаем конфигурацию
                var configService = new FamilyConfigService();
                var formDefinition = configService.LoadFamilyConfig(familySymbol);

                if (formDefinition == null)
                {
                    TaskDialog.Show(Messages.TitleError, Messages.ErrorConfigNotFound);
                    return Result.Cancelled;
                }

                var window = new TypeMagicWindow(formDefinition, familySymbol, doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(Messages.TitleError, string.Format(Messages.ErrorPluginExecution, ex.Message));
                return Result.Failed;
            }
        }
        #endregion

        #region Private Methods
        // Получает FamilySymbol из выбранного элемента (экземпляра или типа в Project Browser)
        private FamilySymbol GetSelectedFamilySymbol(UIDocument uiDoc)
        {
            var doc = uiDoc.Document;
            var selection = uiDoc.Selection;
            var selectedIds = selection.GetElementIds();

            if (selectedIds.Count == 0)
                return null;

            var element = doc.GetElement(selectedIds.First());

            // Если выбран экземпляр семейства
            if (element is FamilyInstance familyInstance)
            {
                return familyInstance.Symbol;
            }

            // Если выбран тип семейства напрямую (например, из Project Browser)
            if (element is FamilySymbol familySymbol)
            {
                return familySymbol;
            }

            return null;
        }
        #endregion
    }

    // Фильтр для выбора только экземпляров семейств
    public class FamilyInstanceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is FamilyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
