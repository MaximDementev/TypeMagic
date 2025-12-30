using System;
using System.Windows;

namespace TypeMagic.UI
{
    // Simple dialog for text input
    public partial class InputDialog : Window
    {
        #region Properties
        public string InputText
        {
            get => txtInput.Text;
            set => txtInput.Text = value;
        }
        #endregion

        #region Constructor
        // Конструктор с текстом подсказки
        public InputDialog(string prompt)
        {
            InitializeComponent();
            txtPrompt.Text = prompt;

            // Динамический расчет ширины окна и высоты TextBox
            txtPrompt.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textHeight = txtPrompt.DesiredSize.Height;
            double textWidth = txtPrompt.DesiredSize.Width;

            // Увеличиваем окно, если текст большой
            this.Width = Math.Max(this.Width, textWidth + 500);

        }

        #endregion

        #region Event Handlers
        // Обработчик кнопки OK
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // Обработчик кнопки Отмена
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        #endregion
    }
}
