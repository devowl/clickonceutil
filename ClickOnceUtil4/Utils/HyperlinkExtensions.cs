﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace ClickOnceUtil4UI.Utils
{
    /// <summary>
    /// http://stackoverflow.com/questions/10238694/example-using-hyperlink-in-wpf
    /// </summary>
    public static class HyperlinkExtensions
    {
        public static readonly DependencyProperty IsExternalProperty = DependencyProperty.RegisterAttached(
            "IsExternal",
            typeof(bool),
            typeof(HyperlinkExtensions),
            new UIPropertyMetadata(false, OnIsExternalChanged));

        public static bool GetIsExternal(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsExternalProperty);
        }

        public static void SetIsExternal(DependencyObject obj, bool value)
        {
            obj.SetValue(IsExternalProperty, value);
        }

        private static void OnIsExternalChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var hyperlink = sender as Hyperlink;

            if ((bool)args.NewValue)
            {
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
            }
            else
            {
                hyperlink.RequestNavigate -= Hyperlink_RequestNavigate;
            }
        }

        private static void Hyperlink_RequestNavigate(
            object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}