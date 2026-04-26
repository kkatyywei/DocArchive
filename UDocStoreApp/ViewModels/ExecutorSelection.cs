using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using UDocStoreApp.Data;
using UDocStoreApp.Infrastructure;
using UDocStoreApp.Models;


namespace UDocStoreApp.ViewModels
{
    public class ExecutorSelection : ViewModelBase
    {
        public int id { get; set; }
        public string FIO { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }
}
