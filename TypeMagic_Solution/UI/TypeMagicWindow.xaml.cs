using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TypeMagic.Constants;
using TypeMagic.Models;
using TypeMagic.Services;
using Control = System.Windows.Controls.Control;
using Grid = System.Windows.Controls.Grid;

namespace TypeMagic.UI
{
    // WPF window for editing family type parameters with dynamic UI generation
    public partial class TypeMagicWindow : Window
    {
        #region Fields
        private readonly FormDefinition _formDefinition;
        private FamilySymbol _familySymbol;
        private readonly Document _document;
        private readonly ParameterApplyService _applyService;
        private Dictionary<string, Control> _inputControls;
        #endregion

        #region Constructor
        // Конструктор окна с передачей данных конфигурации и семейства
        public TypeMagicWindow(FormDefinition formDefinition, FamilySymbol familySymbol, Document document)
        {
            InitializeComponent();

            _formDefinition = formDefinition;
            _familySymbol = familySymbol;
            _document = document;
            _applyService = new ParameterApplyService();
            _inputControls = new Dictionary<string, Control>();

            InitializeWindow();
        }
        #endregion

        #region Initialization
        // Инициализирует окно и строит UI
        private void InitializeWindow()
        {
            txtFamilyName.Text = _familySymbol.FamilyName;
            txtFamilyType.Text = _familySymbol.Name;
            txtVersion.Text = _formDefinition.Version;
            txtStatus.Text = "Готов к редактированию";

            BuildDynamicUI();
        }

        // Строит динамический UI на основе конфигурации
        private void BuildDynamicUI()
        {
            groupsContainer.Children.Clear();

            foreach (var group in _formDefinition.Groups)
            {
                var groupPanel = CreateGroupPanel(group);
                groupsContainer.Children.Add(groupPanel);
            }
        }

        // Создает панель для одной группы параметров
        private Border CreateGroupPanel(GroupDefinition group)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(15),
                Background = Brushes.White
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Левая часть - изображение 400x400
            var imagePanel = CreateImagePanel(group);
            Grid.SetColumn(imagePanel, 0);
            mainGrid.Children.Add(imagePanel);

            // Правая часть - таблица параметров
            var parametersPanel = CreateParametersPanel(group);
            Grid.SetColumn(parametersPanel, 1);
            mainGrid.Children.Add(parametersPanel);

            border.Child = mainGrid;
            return border;
        }

        // Создает панель с изображением группы
        private Border CreateImagePanel(GroupDefinition group)
        {
            var border = new Border
            {
                Style = (Style)FindResource("GroupImageBorderStyle")
            };

            if (!string.IsNullOrEmpty(group.ImagePath) && File.Exists(group.ImagePath))
            {
                var bitmap = new BitmapImage(new Uri(group.ImagePath));

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                border.Child = image;

                border.MouseLeftButtonUp += (_, __) =>
                {
                    OpenImagePreview(bitmap);
                };
            }
            else
            {
                border.Child = CreateImagePlaceholder(group.Name);
            }

            return border;
        }

        private void OpenImagePreview(BitmapImage image)
        {
            var preview = new ImagePreviewWindow(image)
            {
                Owner = this
            };
            preview.ShowDialog();
        }

        private UIElement CreateImagePlaceholder(string name)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = "🖼",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = name,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 350
            });

            return stack;
        }

        // Создает панель с параметрами группы
        private StackPanel CreateParametersPanel(GroupDefinition group)
        {
            var panel = new StackPanel();

            // Заголовок группы
            var header = new TextBlock
            {
                Text = group.Name,
                Style = (Style)FindResource("GroupHeaderStyle")
            };
            panel.Children.Add(header);

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddHeaderCell(headerGrid, "Параметр", 0);
            AddHeaderCell(headerGrid, "Значение", 1);
            AddHeaderCell(headerGrid, "Комментарий", 2);

            panel.Children.Add(headerGrid);

            // Параметры
            foreach (var field in group.Fields)
            {
                var fieldPanel = CreateFieldPanel(field);
                panel.Children.Add(fieldPanel);
            }

            return panel;
        }

        private void AddHeaderCell(Grid grid, string text, int column)
        {
            var cell = new TextBlock
            {
                Text = text,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#555555")),
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }

        private Grid CreateFieldPanel(FieldDefinition field)
        {
            var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Колонка "Параметр" - Label, ParamName, ограничения на разных строках
            var paramStack = new StackPanel { Margin = new Thickness(5) };

            var label = new TextBlock
            {
                Text = field.Label,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            paramStack.Children.Add(label);

            var paramName = new TextBlock
            {
                Text = field.ParamName,
                Style = (Style)FindResource("ParamNameStyle")
            };
            paramStack.Children.Add(paramName);

            Grid.SetColumn(paramStack, 0);
            grid.Children.Add(paramStack);

            // Колонка "Значение" - поле ввода + ограничения справа
            var valueStack = new StackPanel { Margin = new Thickness(5) };

            var valueControl = CreateInputControl(field);
            valueControl.Tag = field;
            valueStack.Children.Add(valueControl);

            var constraintsText = BuildConstraintsText(field);
            if (!string.IsNullOrWhiteSpace(constraintsText))
            {
                var constraints = new TextBlock
                {
                    Text = constraintsText,
                    Style = (Style)FindResource("ConstraintsStyle")
                };
                valueStack.Children.Add(constraints);
            }

            Grid.SetColumn(valueStack, 1);
            grid.Children.Add(valueStack);

            var commentBlock = new TextBlock
            {
                Text = field.Comment ?? string.Empty,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(commentBlock, 2);
            grid.Children.Add(commentBlock);

            return grid;
        }

        private FrameworkElement CreateInputControl(FieldDefinition field)
        {
            // Получаем текущее значение параметра
            var currentValue = GetCurrentParameterValue(field);

            Control inputControl = null;

            switch (field.UiType)
            {
                case FieldType.CheckBox:
                    var checkBox = new CheckBox
                    {
                        IsChecked = currentValue is int intVal && intVal != 0,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5)
                    };
                    inputControl = checkBox;
                    break;

                case FieldType.Integer:
                case FieldType.Double:
                case FieldType.String:
                    string displayValue = string.Empty;
                    if (currentValue != null)
                    {
                        if (field.UiType == FieldType.Double && currentValue is double dblVal)
                        {
                            displayValue = dblVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            displayValue = currentValue.ToString();
                        }
                    }

                    if (field.Options != null && field.Options.Any())
                    {
                        var comboBox = new System.Windows.Controls.ComboBox
                        {
                            Style = (Style)FindResource("InputStyle"),
                            ItemsSource = field.Options,
                            Text = displayValue,
                            IsEditable = true
                        };
                        inputControl = comboBox;
                    }
                    else
                    {
                        if (field.UiType == FieldType.String)
                        {
                            var expandableTextBox = new ExpandableTextBox
                            {
                                Text = displayValue
                            };
                            inputControl = expandableTextBox;
                        }
                        else
                        {
                            var textBox = new System.Windows.Controls.TextBox
                            {
                                Style = (Style)FindResource("InputStyle"),
                                Text = displayValue
                            };
                            inputControl = textBox;
                        }
                    }
                    break;

                case FieldType.ElementId:
                    var elementComboBox = new System.Windows.Controls.ComboBox
                    {
                        Style = (Style)FindResource("InputStyle"),
                        ItemsSource = GetElementIdOptions(field.Prefix),
                        IsEditable = true
                    };

                    // Настраиваем шаблон отображения элемента
                    var itemTemplate = new DataTemplate();
                    var factory = new FrameworkElementFactory(typeof(TextBlock));

                    // Создаем привязку для отображения "(FamilyName) TypeName"
                    var binding = new System.Windows.Data.Binding();
                    binding.Converter = new ElementTypeDisplayConverter();
                    factory.SetBinding(TextBlock.TextProperty, binding);

                    itemTemplate.VisualTree = factory;
                    elementComboBox.ItemTemplate = itemTemplate;

                    if (currentValue is ElementId elemId)
                    {
                        var currentElement = _document.GetElement(elemId);
                        if (currentElement != null)
                            elementComboBox.SelectedItem = currentElement;
                    }

                    inputControl = elementComboBox;
                    break;

                default:
                    inputControl = new System.Windows.Controls.TextBox { Style = (Style)FindResource("InputStyle") };
                    break;
            }

            if (inputControl != null && !string.IsNullOrEmpty(field.ParamName))
            {
                _inputControls[field.ParamName] = inputControl;
            }

            return inputControl;
        }

        private object GetCurrentParameterValue(FieldDefinition field)
        {
            var param = _familySymbol.LookupParameter(field.ParamName);
            if (param == null)
                return null;

            switch (field.UiType)
            {
                case FieldType.Integer:
                    {
                        var valueString = param.AsValueString();
                        if (!string.IsNullOrEmpty(valueString))
                        {
                            var cleanValue = CleanNumericString(valueString);
                            if (int.TryParse(cleanValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int intVal))
                                return intVal;
                        }
                        return param.AsInteger();
                    }
                case FieldType.CheckBox:
                    return param.AsInteger();
                case FieldType.Double:
                    {
                        // Получаем значение в футах (внутренние единицы Revit)
                        var value = param.AsDouble();

                        // Получаем тип единиц измерения параметра
                        var specTypeId = param.Definition.GetDataType();

                        // Параметры арматуры уже хранятся в миллиметрах
                        if (specTypeId == SpecTypeId.BarDiameter || specTypeId == SpecTypeId.ReinforcementLength)
                        {
                            return Math.Round(UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters), 0);
                        }

                        // Преобразуем в отображаемые единицы если это не безразмерное значение
                        if (specTypeId != SpecTypeId.Number)
                        {
                            try
                            {
                                // Для Length используем миллиметры
                                if (specTypeId == SpecTypeId.Length)
                                {
                                    var mmValue = UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters);
                                    return Math.Round(mmValue, 0);
                                }
                                // Для других типов используем стандартные единицы
                                else
                                {
                                    var displayUnits = UnitUtils.GetValidUnits(specTypeId);
                                    if (displayUnits != null && displayUnits.Any())
                                    {
                                        var defaultUnit = displayUnits.First();
                                        value = UnitUtils.ConvertFromInternalUnits(value, defaultUnit);
                                    }
                                }
                            }
                            catch
                            {
                                // Если преобразование не удалось, используем значение как есть
                            }
                        }

                        return value;
                    }
                case FieldType.String:
                    return param.AsString();
                case FieldType.ElementId:
                    return param.AsElementId();
                default:
                    return null;
            }
        }

        private string BuildConstraintsText(FieldDefinition field)
        {
            var parts = new List<string>();

            if (field.Min.HasValue)
                parts.Add($"Min: {field.Min.Value}");

            if (field.Max.HasValue)
                parts.Add($"Max: {field.Max.Value}");

            if (!string.IsNullOrWhiteSpace(field.Prefix))
                parts.Add($"Префикс: {field.Prefix}");

            if (field.Options != null && field.Options.Any())
                parts.Add($"Варианты: {string.Join(", ", field.Options)}");

            return string.Join(" | ", parts);
        }

        // Получает список элементов для ElementId с учетом префикса
        private List<Element> GetElementIdOptions(string prefix)
        {
            var collector = new FilteredElementCollector(_document)
                .WhereElementIsElementType();

            if (!string.IsNullOrEmpty(prefix))
            {
                return collector
                    .OfType<ElementType>()
                    .Where(t =>
                        !string.IsNullOrEmpty(t.FamilyName) &&
                        t.FamilyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Cast<Element>()
                    .ToList();
            }

            return collector.ToList();
        }

        #endregion

        #region Event Handlers
        // Обработчик кнопки "Применить"
        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var values = CollectParameterValues();

                var convertedValues = CollectParameterValuesForApply();

                using (var transaction = new Transaction(_document, "Применение параметров"))
                {
                    transaction.Start();
                    _applyService.ApplyParameters(_familySymbol, convertedValues, _document);
                    transaction.Commit();
                }

                txtStatus.Text = Messages.InfoApplySuccess;
                this.Topmost = false;
                TaskDialog.Show(Messages.TitleInfo, Messages.InfoApplySuccess);
                this.Topmost = true;
            }
            catch (Exception ex)
            {
                this.Topmost = false;
                TaskDialog.Show(Messages.TitleError, string.Format(Messages.ErrorApply, ex.Message));
                this.Topmost = true;
            }
        }

        // Обработчик кнопки "Создать новый тип"
        private void BtnCreateNewType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputDialog = new InputDialog("Введите имя нового типа");
                inputDialog.Owner = this;
                if (inputDialog.ShowDialog() != true)
                    return;

                var newTypeName = inputDialog.InputText;
                if (string.IsNullOrWhiteSpace(newTypeName))
                    return;

                var values = CollectParameterValues();

                var convertedValues = CollectParameterValuesForApply();

                FamilySymbol newSymbol = null;

                using (var transaction = new Transaction(_document, "Создание нового типа"))
                {
                    transaction.Start();
                    newSymbol = _applyService.CreateNewType(_familySymbol, newTypeName, _document);
                    _applyService.ApplyParameters(newSymbol, convertedValues, _document);
                    transaction.Commit();
                }

                this.Topmost = false;
                TaskDialog.Show(Messages.TitleInfo, string.Format(Messages.InfoNewTypeCreated, newTypeName));
                this.Topmost = true;

                // Обновляем выбранный тип
                _familySymbol = newSymbol;
            }
            catch (Exception ex)
            {
                this.Topmost = false;
                TaskDialog.Show(Messages.TitleError, string.Format(Messages.ErrorCreateType, ex.Message));
                this.Topmost = true;
            }
        }

        // Обработчик кнопки "Отмена"
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Собирает значения параметров из контролов UI
        private Dictionary<string, object> CollectParameterValues()
        {
            var values = new Dictionary<string, object>();

            foreach (var group in _formDefinition.Groups)
            {
                foreach (var field in group.Fields)
                {
                    if (!_inputControls.ContainsKey(field.ParamName))
                        continue;

                    var control = _inputControls[field.ParamName];
                    var value = ExtractValueFromControl(control, field, convertUnits: false);

                    if (value != null)
                        values[field.ParamName] = value;
                }
            }

            return values;
        }

        private object ExtractValueFromControl(Control control, FieldDefinition field, bool convertUnits = true)
        {
            if (control is CheckBox checkBox)
            {
                return checkBox.IsChecked == true ? 1 : 0;
            }
            else if (control is System.Windows.Controls.ComboBox comboBox)
            {
                if (field.UiType == FieldType.ElementId)
                {
                    var element = comboBox.SelectedItem as Element;
                    return element?.Id;
                }
                else
                {
                    var selectedValue = comboBox.IsEditable && !string.IsNullOrEmpty(comboBox.Text)
                        ? comboBox.Text
                        : comboBox.SelectedItem?.ToString();
                    return ParseValue(field, selectedValue, convertUnits);
                }
            }
            else if (control is ExpandableTextBox expandableTextBox)
            {
                return ParseValue(field, expandableTextBox.Text, convertUnits);
            }
            else if (control is System.Windows.Controls.TextBox textBox)
            {
                return ParseValue(field, textBox.Text, convertUnits);
            }

            return null;
        }

        private object ParseValue(FieldDefinition field, string value, bool convertUnits = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (field.UiType == FieldType.String)
                    return string.Empty;
                return null;
            }

            // Для строк возвращаем как есть
            if (field.UiType == FieldType.String)
                return value;

            // Очищаем от пробелов и букв
            value = CleanNumericString(value);
            value = value.Replace(',', '.');

            switch (field.UiType)
            {
                case FieldType.Integer:
                    return int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int intVal) ? intVal : (object)null;

                case FieldType.Double:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleVal))
                    {
                        if (!convertUnits)
                        {
                            return doubleVal;
                        }

                        // Получаем параметр для определения его типа единиц
                        var param = _familySymbol.LookupParameter(field.ParamName);
                        if (param != null)
                        {
                            var specTypeId = param.Definition.GetDataType();

                            // Параметры арматуры уже используют миллиметры - не нужна конвертация
                            if (specTypeId == SpecTypeId.BarDiameter || specTypeId == SpecTypeId.ReinforcementLength)
                            {
                                return Math.Round(doubleVal, 0);
                            }

                            // Конвертируем во внутренние единицы (футы) если это не безразмерное значение
                            if (specTypeId != SpecTypeId.Number)
                            {
                                try
                                {
                                    // Для Length конвертируем из миллиметров
                                    if (specTypeId == SpecTypeId.Length)
                                    {
                                        doubleVal = Math.Round(doubleVal, 0);
                                        return UnitUtils.ConvertToInternalUnits(doubleVal, UnitTypeId.Millimeters);
                                    }
                                    // Для других типов конвертируем из стандартных единиц
                                    else
                                    {
                                        var displayUnits = UnitUtils.GetValidUnits(specTypeId);
                                        if (displayUnits != null && displayUnits.Any())
                                        {
                                            var defaultUnit = displayUnits.First();
                                            return UnitUtils.ConvertToInternalUnits(doubleVal, defaultUnit);
                                        }
                                    }
                                }
                                catch
                                {
                                    // Если преобразование не удалось, используем значение как есть
                                }
                            }
                        }
                        return doubleVal;
                    }
                    return null;

                default:
                    return null;
            }
        }

        private Dictionary<string, object> CollectParameterValuesForApply()
        {
            var values = new Dictionary<string, object>();

            foreach (var group in _formDefinition.Groups)
            {
                foreach (var field in group.Fields)
                {
                    if (!_inputControls.ContainsKey(field.ParamName))
                        continue;

                    var control = _inputControls[field.ParamName];
                    // Extract value WITH unit conversion
                    var value = ExtractValueFromControl(control, field, convertUnits: true);

                    if (value != null || field.UiType == FieldType.String)
                        values[field.ParamName] = value;
                }
            }

            return values;
        }
        #endregion

        private string CleanNumericString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Удаляем все символы кроме цифр, точки, запятой, минуса и плюса
            var result = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (char.IsDigit(c) || c == '.' || c == ',' || c == '-' || c == '+')
                {
                    result.Append(c);
                }
            }
            result = result.Replace(',', '.');
            return result.ToString();
        }
    }
}
