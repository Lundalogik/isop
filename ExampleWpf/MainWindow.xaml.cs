﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Isop.WpfControls.ViewModels;

namespace Isop.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MethodTreeModel MethodTreeModel { get; set; }
        public ObservableCollection<Param> Parameters;
        public Method CurrentMethod { get; set; }
        protected Build ParserBuilder { get; set; }

        public MainWindow()
        {
            //Console.SetOut
            ParserBuilder = new Build()
                     .Parameter("global")
                     .RecognizeHelp()
                     .Recognize(typeof(MyController))
                     .Recognize(typeof(CustomerController));

            MethodTreeModel = ParserBuilder.GetMethodTreeModel();

            InitializeComponent();
            paramview.Source = MethodTreeModel.GlobalParameters;
            controllersAndCommands.DataContext = MethodTreeModel.Controllers;
            textBlock1.Text = string.Empty;
        }

        private void SelectedMethodChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Method)
            {
                CurrentMethod = (Method)e.NewValue;
                methodview.DataContext = e.NewValue;
                methodview.Source = CurrentMethod.Parameters;
                textBlock1.Text = string.Empty;
            }
        }

        private void ExecuteMethodButtonClick(object sender, RoutedEventArgs e)
        {
            var cout = new StringWriter(CultureInfo.CurrentCulture);
            try
            {
                MethodTreeModel.GlobalParameters.GetParsedArguments().Invoke(cout);
            }
            catch (MissingArgumentException ex)
            {
                textBlock1.Text = String.Format("Missing argument(s): {0}", String.Join(", ", ex.Arguments.Select(a => String.Format("{0}: {1}", a.Key, a.Value)).ToArray()));
                return;
            }
#if DEBUG
            catch (Exception ex1)
            {
                textBlock1.Text =string.Join(Environment.NewLine, new object[]{ 
                    "The global parameter invokation failed with exception:",
                    ex1.Message, ex1.StackTrace});
                return;
            }
#endif
            if (null == CurrentMethod) return;

            cout.WriteLine();
            try
            {
                var parsedMethod = ParserBuilder.Parse(CurrentMethod);
                parsedMethod.Invoke(cout);
            }
            catch (MissingArgumentException ex)
            {
                textBlock1.Text = String.Format("Missing argument(s): {0}", String.Join(", ", ex.Arguments.Select(a => String.Format("{0}: {1}", a.Key, a.Value)).ToArray()));
                return;
            }
#if DEBUG
            catch (Exception ex1)
            {
                textBlock1.Text = string.Join(Environment.NewLine, new object[] { 
                    "The invokation of the action failed with exception:", 
                    ex1.Message, ex1.StackTrace });
                return;
            }
#endif
            textBlock1.Text = cout.ToString();
        }
    }
}