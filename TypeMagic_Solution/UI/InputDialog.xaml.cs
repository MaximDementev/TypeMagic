using System.Windows;

namespace TypeMagic.UI
{
    // Simple dialog for text input
    public partial class InputDialog : Window
    {
        #region Properties
        public string InputText { get; private set; }
        #endregion

        #region Constructor
        // Конструктор с текстом подсказки
        public InputDialog(string prompt)
        {
            InitializeComponent();
            txtPrompt.Text = prompt;
        }
        #endregion

        #region Event Handlers
        // Обработчик кнопки OK
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
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
