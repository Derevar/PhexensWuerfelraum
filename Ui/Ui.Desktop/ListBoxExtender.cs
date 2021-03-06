﻿using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace PhexensWuerfelraum.Ui.Desktop
{
    public class ListBoxExtender : DependencyObject
    {
        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ListBoxExtender), new UIPropertyMetadata(default(bool), OnAutoScrollToEndChanged));

        /// <summary>
        /// Returns the value of the AutoScrollToEndProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be returned</param>
        /// <returns>The value of the given property</returns>
        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToEndProperty);
        }

        /// <summary>
        /// Sets the value of the AutoScrollToEndProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be set</param>
        /// <param name="value">The value which should be assigned to the AutoScrollToEndProperty</param>
        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

        /// <summary>
        /// This method will be called when the AutoScrollToEnd
        /// property was changed
        /// </summary>
        /// <param name="s">The sender (the ListBox)</param>
        /// <param name="e">Some additional information</param>
        public static void OnAutoScrollToEndChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var listBox = s as ListBox;
            if (listBox != null)
            {
                var listBoxItems = listBox.Items;
                var data = listBoxItems.SourceCollection as INotifyCollectionChanged;

                var scrollToEndHandler = new NotifyCollectionChangedEventHandler(
                    (s1, e1) =>
                    {
                        if (listBox.Items.Count > 0)
                        {
                            listBoxItems.MoveCurrentToLast();
                            listBox.ScrollIntoView(listBoxItems.CurrentItem);
                        }
                    });

                if ((bool)e.NewValue)
                    data.CollectionChanged += scrollToEndHandler;
                else
                    data.CollectionChanged -= scrollToEndHandler;
            }
        }
    }
}