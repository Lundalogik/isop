﻿using System.Windows;
using Isop.WpfControls.ViewModels;
using System.Collections.Generic;

namespace Isop.WpfControls
{
    // http://stackoverflow.com/questions/2814866/programmatically-add-controls-to-wpf-form
    /// <summary>
    /// Interaction logic for MethodView.xaml
    /// </summary>
    public partial class MethodView
    {

        public IEnumerable<Param> Source
        {
            set { FieldsListBox.ItemsSource = value; }
        }

        public MethodView()
        {
            InitializeComponent();
        }
    }
}
