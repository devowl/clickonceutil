﻿using ClickOnceUtil4UI.UI.ViewModels;

namespace ClickOnceUtil4UI.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Создание экземпляра класса <see cref="MainWindow"/>.
        /// </summary>
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
        }
    }
}