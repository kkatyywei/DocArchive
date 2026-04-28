using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UDocStoreApp.ViewModels;

namespace UDocStoreApp.Models
{
    public class ExecutorSelection : ViewModelBase
    {
        public int id { get; set; }
        public string FIO { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }
}
