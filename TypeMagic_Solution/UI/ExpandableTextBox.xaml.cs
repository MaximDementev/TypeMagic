using System.Windows;
using System.Windows.Controls;

namespace TypeMagic.UI
{
    public partial class ExpandableTextBox : UserControl
    {
        private bool _isExpanded = false;

        public ExpandableTextBox()
        {
            InitializeComponent();
        }

        // Свойство для доступа к тексту
        public string Text
        {
            get => txtMain.Text;
            set
            {
                txtMain.Text = value;
                txtExpanded.Text = value;
            }
        }

        // Событие изменения текста
        public event TextChangedEventHandler TextChanged;

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;

            if (_isExpanded)
            {
                // Раскрываем многострочное поле
                txtExpanded.Visibility = Visibility.Visible;
                txtExpanded.Text = txtMain.Text;
                txtMain.IsReadOnly = true;
                btnToggle.Content = "▲";
            }
            else
            {
                // Сворачиваем многострочное поле
                txtExpanded.Visibility = Visibility.Collapsed;
                txtMain.IsReadOnly = false;
                btnToggle.Content = "✎";
            }
        }

        private void TxtMain_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isExpanded)
            {
                // Синхронизация с развернутым полем
                txtExpanded.Text = txtMain.Text;
                TextChanged?.Invoke(this, e);
            }
        }

        private void TxtExpanded_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isExpanded)
            {
                // Синхронизация с основным полем
                txtMain.Text = txtExpanded.Text;
                TextChanged?.Invoke(this, e);
            }
        }
    }
}
