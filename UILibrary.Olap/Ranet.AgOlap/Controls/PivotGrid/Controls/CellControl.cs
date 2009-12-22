﻿/*   
    Copyright (C) 2009 Galaktika Corporation ZAO

    This file is a part of Ranet.UILibrary.Olap
 
    Ranet.UILibrary.Olap is a free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
      
    You should have received a copy of the GNU General Public License
    along with Ranet.UILibrary.Olap.  If not, see
  	<http://www.gnu.org/licenses/> 
  
    If GPL v.3 is not suitable for your products or company,
    Galaktika Corp provides Ranet.UILibrary.Olap under a flexible commercial license
    designed to meet your specific usage and distribution requirements.
    If you have already obtained a commercial license from Galaktika Corp,
    you can use this file under those license terms.
*/

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Ranet.AgOlap.Controls.General;
using System.Windows.Browser;
using Ranet.AgOlap.Controls.PivotGrid.Controls;
using System.Collections.Generic;
using Ranet.Olap.Core.Providers;
using Ranet.AgOlap.Controls.PivotGrid.Conditions;

namespace Ranet.AgOlap.Controls.PivotGrid.Controls
{
    public class CellClickEventArgs : EventArgs
    {
        public CellClickEventArgs(
            CellControl cell)
        {
            this.Cell = cell;
        }

        public CellClickEventArgs(
            CellControl cell, Point point)
            : this(cell)
        {
            this.Position = point;
        }

        public readonly CellControl Cell = null;
        public readonly Point Position = default(Point);
    }

    public class CellControl : PivotGridItem
    {
        public const int CORNER_RADIUS = 2;

        CellInfo m_Cell = null;
        public CellInfo Cell
        {
            get { return m_Cell; }
            set {
                if (m_Cell != value)
                {
                    m_Cell = value;
                    // Ищем условия, которым может соответствовать ячейка
                    CalculateCustomCellCondidtions();

                    ApplySettings();
                }
            }
        }

        //public readonly int RowIndex = -1;
        //public readonly int ColumnIndex = -1;

        Border m_Border = null;

        protected readonly PivotGridControl Owner = null;
        Grid m_LayoutPanel;
        TextBlock m_Caption_Text;

        public CellControl(PivotGridControl owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
            Owner = owner;

            m_Border = new Border();
            m_Border.CornerRadius = new CornerRadius(CORNER_RADIUS);

            this.MouseLeftButtonDown += new MouseButtonEventHandler(CellControl_MouseLeftButtonDown);

            m_LayoutPanel = new Grid();
            m_LayoutPanel.Margin = new Thickness(0);
            m_LayoutPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            m_LayoutPanel.ColumnDefinitions.Add(new ColumnDefinition());
            m_LayoutPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            resDoubleClickTimer = new Storyboard();
            resDoubleClickTimer.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 250));
            resDoubleClickTimer.Completed += new EventHandler(Storyboard_Completed);
            m_Border.Resources.Add("resDoubleClickTimer", resDoubleClickTimer);

            // Текст
            m_Caption_Text = new TextBlock();
            m_Caption_Text.FontSize = Owner.DefaultFontSize * Scale;
            m_Caption_Text.HorizontalAlignment = HorizontalAlignment.Stretch;
            m_Caption_Text.VerticalAlignment = VerticalAlignment.Center;
            m_Caption_Text.TextAlignment = TextAlignment.Right;
            m_Caption_Text.Margin = new Thickness(0, 0, 3 * Scale, 0);
            m_LayoutPanel.Children.Add(m_Caption_Text);
            Grid.SetColumn(m_Caption_Text, 1);

            m_Border.Child = m_LayoutPanel;
            this.Content = m_Border;

        }

        Image m_Image = null;
        ProgressBar m_ProgressBar = null;

        void CellControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_ClickCount++;
            m_LastArgs = e;
            resDoubleClickTimer.Begin();
        }

        Storyboard resDoubleClickTimer;
        private void Storyboard_Completed(object sender, EventArgs e)
        {
            if (m_ClickCount > 1)
            {
                this.OnMouseDoubleClick(m_LastArgs);
            }
            m_ClickCount = 0;
        }

        public event EventHandler MouseDoubleClick;
        protected void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            EventHandler handler = MouseDoubleClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        int m_ClickCount = 0;
        MouseButtonEventArgs m_LastArgs;

        void ApplySettings()
        {
//            CalculateCustomCellCondidtions();

            ApplyBorderSettings();

            ApplyBackgroundSettings();

            ApplyValueSettings();

//            ApplyHintSettings();
        }

        /// <summary>
        /// Условия, под которые попадает ячейка
        /// </summary>
        IList<CellCondition> m_CustomCellCondidtions = null;
        /// <summary>
        /// Суммарные свойства ячейки с учетом наложения нескольких условий
        /// </summary>
        CellAppearanceObject m_CustomCellAppearance = null;

        void CalculateCustomCellCondidtions()
        {
            m_CustomCellCondidtions = null;
            m_CustomCellAppearance = null;

            if (Cell != null)
            {
                if (Owner != null && Owner.CustomCellsConditions != null && Owner.CustomCellsConditions.Count > 0)
                {
                    // Поиск условий, которые могут быть наложены на данную ячейку. 
                    // Перебираем всех предков по области столбцов и по их уникальному имени ищем
                    IList<MemberInfo> column_ascendants = new List<MemberInfo>();
                    this.Cell.ColumnMember.CollectAncestors(column_ascendants);
                    // column_ascendants - коллекция предков снизу вверх
                    foreach (MemberInfo member in column_ascendants)
                    {
                        // Ищем объект, содержащий условия для данного элемента по уникальному имени элемента
                        CellConditionsDescriptor conditions_descr = null;
                        foreach (CellConditionsDescriptor descr in Owner.CustomCellsConditions)
                        {
                            if (descr.MemberUniqueName == member.UniqueName)
                            {
                                conditions_descr = descr;
                                break;
                            }
                        }

                        List<CellCondition> usedConditions = null;
                        // Отбор условий, которым удовлетворяет значение ячейки
                        if (conditions_descr != null)
                        {
                            if (Cell.CellDescr != null &&
                                Cell.CellDescr.Value != null &&
                                Cell.CellDescr.Value.Value != null)
                            {
                                try
                                {
                                    double value = Convert.ToDouble(Cell.CellDescr.Value.Value);
                                    usedConditions = CellConditionsDescriptor.TestToConditions(value, conditions_descr);
                                }
                                catch (System.InvalidCastException)
                                {
                                }
                            }
                        }

                        if (usedConditions != null)
                        {
                            //Условий может быть несколько и они могут накладываться, например:
                            //1) > 0 и цвет фона - красный. 
                            //2) > 0 и шрифт жирный
                            //Поэтому для храненния накопленных условий использую m_CustomCellAppearance и потом буду использовать эту информацию при рисовании ячейки

                            //Проходим по списку условий и применяем настройки для ячейки
                            foreach (CellCondition cond in usedConditions)
                            {
                                if (m_CustomCellAppearance == null)
                                    m_CustomCellAppearance = new CellAppearanceObject();

                                if (cond.Appearance.Options.IgnoreAllOptions)
                                    continue;

                                if (cond.Appearance.Options.UseAllOptions ||
                                    cond.Appearance.Options.UseBackColor)
                                {
                                    m_CustomCellAppearance.Options.UseBackColor = true;
                                    m_CustomCellAppearance.BackColor = cond.Appearance.BackColor;
                                    //painter.CellArgs.StyleAppearance.BackColor2 =
                                    //    cond.Appearance.BackColor2;
                                    //painter.CellArgs.StyleAppearance.GradientMode =
                                    //    cond.Appearance.GradientMode;
                                }
                                if (cond.Appearance.Options.UseAllOptions ||
                                    cond.Appearance.Options.UseBorderColor)
                                {
                                    m_CustomCellAppearance.Options.UseBorderColor = true;
                                    m_CustomCellAppearance.BorderColor = cond.Appearance.BorderColor;
                                }
                                //if (cond.Appearance.Options.UseAllOptions ||
                                //    cond.Appearance.Options.UseFont)
                                //{
                                //    if (cond.Appearance.Font != null)
                                //    {
                                //        painter.CellArgs.StyleAppearance.Options.UseFont
                                //            = true;
                                //        painter.CellArgs.StyleAppearance.Font =
                                //            cond.Appearance.Font;
                                //    }
                                //}
                                if (cond.Appearance.Options.UseAllOptions ||
                                    cond.Appearance.Options.UseForeColor)
                                {
                                    m_CustomCellAppearance.Options.UseForeColor = true;
                                    m_CustomCellAppearance.ForeColor = cond.Appearance.ForeColor;
                                }

                                if (cond.Appearance.Options.UseAllOptions ||
                                    !cond.Appearance.Options.ShowValue)
                                {
                                    m_CustomCellAppearance.Options.ShowValue = false;
                                }

                                //if (cond.Appearance.Options.UseAllOptions ||
                                //    cond.Appearance.Options.UseTextOptions)
                                //{
                                //    painter.CellArgs.StyleAppearance.Options.
                                //        UseTextOptions = true;

                                //    painter.CellArgs.StyleAppearance.TextOptions.
                                //        HAlignment =
                                //        cond.Appearance.TextOptions.HAlignment;
                                //    painter.CellArgs.StyleAppearance.TextOptions.
                                //        VAlignment =
                                //        cond.Appearance.TextOptions.VAlignment;
                                //    painter.CellArgs.StyleAppearance.TextOptions.
                                //        WordWrap = cond.Appearance.TextOptions.WordWrap;
                                //    painter.CellArgs.StyleAppearance.TextOptions.
                                //        Trimming = cond.Appearance.TextOptions.Trimming;
                                //}

                                if (cond.Appearance.Options.UseAllOptions ||
                                    cond.Appearance.Options.UseImage)
                                {
                                    m_CustomCellAppearance.Options.UseImage = true;
                                    m_CustomCellAppearance.CustomImage = cond.Appearance.CustomImage;
                                }

                                if (cond.Appearance.Options.UseAllOptions ||
                                    cond.Appearance.Options.UseProgressBar)
                                {
                                    m_CustomCellAppearance.Options.UseProgressBar = true;
                                    m_CustomCellAppearance.ProgressBarOptions = cond.Appearance.ProgressBarOptions;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        void ApplyBackgroundSettings()
        {
            if (Cell == null)
            {
                m_Border.Background = new SolidColorBrush(Colors.Transparent);
                return;
            }

            if (IsSelected && !IsFocused)
            {
                m_Border.Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 225));
            }
            else
            {
                // Если ячейка редактируемая
                if (Owner != null && Owner.CanEdit && Cell.IsUpdateable)
                {
                    if (IsModified)
                    {
                        m_Border.Background = new SolidColorBrush(Color.FromArgb(125, Colors.Cyan.R, Colors.Cyan.G, Colors.Cyan.B));
                    }
                    else
                    {
                        // SystemColors.Info
                        m_Border.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 225));
                    }
                }
                else
                {
                    if (m_CustomCellAppearance != null && m_CustomCellAppearance.Options.UseBackColor)
                    {
                        // Фон из настроек условий для ячейки
                        m_Border.Background = new SolidColorBrush(m_CustomCellAppearance.BackColor);
                    }
                    else
                    {
                        if (Cell != null)
                        {
                            int backColor = Cell.CellDescr.BackColor;
                            if (backColor != int.MinValue)
                            {
                                // Фон из OLAP (свойства ячейки)
                                Color color = ColorHelper.FromRgb(backColor);
                                m_Border.Background = new SolidColorBrush(color);
                            }
                            else
                            {
                                // Фон по-умолчанию
                                if (Owner != null)
                                {
                                    if(m_Border.Background != Owner.CellsBackground)
                                        m_Border.Background = Owner.CellsBackground;
                                }
                                else
                                {
                                    m_Border.Background = new SolidColorBrush(Colors.White);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ShowOriginalValue()
        {
            IsModified = false;
            ApplySettings();
        }

        //ToolTipControl m_ToolTip;

        private void ApplyBorderSettings()
        {
            if (Cell == null)
            {
                m_Border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                return;
            }

            if (IsFocused)
            {
                Border border = m_Border;
                border.BorderBrush = new SolidColorBrush(Colors.Black);
                border.BorderThickness = new Thickness(1, 1, 1, 1);
                // Чтобы не сместилось содержимое при появлении левой и верхней границы
                m_LayoutPanel.Margin = new Thickness(-1, -1, 0, 0);
            }
            else
            {
                Border border = m_Border;

                if (m_CustomCellAppearance != null && m_CustomCellAppearance.Options.UseBorderColor)
                {
                    // Цвет из настроек условий для ячейки
                    border.BorderBrush = new SolidColorBrush(m_CustomCellAppearance.BorderColor);
                    // Рамка полностью
                    border.BorderThickness = new Thickness(1, 1, 1, 1);
                    // Чтобы не сместилось содержимое при появлении левой и верхней границы
                    m_LayoutPanel.Margin = new Thickness(-1, -1, 0, 0);
                }
                else
                {
                    if (Owner != null)
                    {
                        if (border.BorderBrush != Owner.CellsBorder)
                            border.BorderBrush = Owner.CellsBorder;
                    }
                    else
                    {
                        border.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                    }
                    border.BorderThickness = new Thickness(0, 0, 1, 1);
                    m_LayoutPanel.Margin = new Thickness(0);
                }
            }
        }

        public void ChangeText(String newText)
        {
            if (m_Caption_Text.Text != newText)
            {
                if (Cell != null)
                    Cell.ModifiedValue = newText;
                IsModified = true;
                ApplyValueSettings();
                ApplyBackgroundSettings();
            }
        }

        public String Text
        {
            get {
                return m_Caption_Text.Text;
            }
        }

        double Scale
        {
            get
            {
                if (Owner == null)
                {
                    return 1;
                }
                else
                {
                    return Owner.Scale;
                }

            }
        }

        private void ApplyValueSettings()
        {
            if (Cell == null)
            {
                m_Caption_Text.Visibility = Visibility.Collapsed;
                if (m_ProgressBar != null)
                {
                    m_ProgressBar.Visibility = Visibility.Collapsed;
                }
                if (m_Image != null)
                {
                    m_Image.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Если в ячейке отображается только значение, то оно должно быть выравнено по правому краю
            // Если в ячейке отображается только картинка, то она должна быть выравнена по центру
            // Если в ячейке отображается картинка и значение, то картинка должна быть выравнена по левому краю, а значение по правому

            bool show_Value = true;
            bool show_Image = false;
            bool show_ProgressBar = false;
            if (m_CustomCellAppearance != null)
            {
                show_Value = m_CustomCellAppearance.Options.ShowValue;
                show_Image = m_CustomCellAppearance.Options.UseImage && m_CustomCellAppearance.CustomImage != null;
                show_ProgressBar = m_CustomCellAppearance.Options.UseProgressBar;
            }
            if (show_ProgressBar == true)
                show_Image = false;

            // Прогрессбар из условия
            if (show_ProgressBar)
            {
                if (m_ProgressBar == null)
                {
                    m_ProgressBar = new ProgressBar();
                    m_ProgressBar.VerticalAlignment = VerticalAlignment.Stretch;
                    m_ProgressBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                    m_LayoutPanel.Children.Add(m_ProgressBar);
                    Grid.SetColumnSpan(m_ProgressBar, 3);
                    
                    // Текстовое поле поверх прогресса
                    m_LayoutPanel.Children.Remove(m_Caption_Text);
                    m_LayoutPanel.Children.Add(m_Caption_Text);
                    Grid.SetColumn(m_Caption_Text, 1);
                }

                m_ProgressBar.Minimum = m_CustomCellAppearance.ProgressBarOptions.MinValue;
                m_ProgressBar.Maximum = m_CustomCellAppearance.ProgressBarOptions.MaxValue;

                double value = 0;
                if (Cell.CellDescr != null &&
                    Cell.CellDescr.Value != null &&
                    Cell.CellDescr.Value.Value != null)
                {
                    try
                    {
                        value = Convert.ToDouble(Cell.CellDescr.Value.Value);
                    }
                    catch (System.InvalidCastException)
                    {
                    }
                }
                m_ProgressBar.Value = value;
                GradientStopCollection stops = new GradientStopCollection();
                stops.Add(new GradientStop() { Color = m_CustomCellAppearance.ProgressBarOptions.StartColor });
                stops.Add(new GradientStop() { Color = m_CustomCellAppearance.ProgressBarOptions.EndColor, Offset = 1 });
                LinearGradientBrush brush = new LinearGradientBrush(stops, 0);
                m_ProgressBar.Foreground = brush;

                m_ProgressBar.Visibility = Visibility.Visible;

                // В прогрессбаре текст отображаем в центре
                m_Caption_Text.TextAlignment = TextAlignment.Center;
            }
            else
            {
                if(m_ProgressBar != null)
                    m_ProgressBar.Visibility = Visibility.Collapsed;
            }

            // Если никаких условий не задано, либо в условии прописано отображать значение
            if (show_Value)
            {
                m_Caption_Text.Visibility = Visibility.Visible;

                if (IsModified)
                {
                    // Пытаемся отобразить модифицированное значение в том же формате, в котором оно будет отображаться пользователю когда запишется в куб
                    // Модифицированное значение пытаемся преобразовать в число. Если преобразование успешное, то пытаемся применить строку форматирования
                    // В случае, если преобразование в число не получится, то выводим модифицированное значение просто в строку
                    // String text = Cell.ModifiedValue;
                    //if (Cell.CellDescr != null && !String.IsNullOrEmpty(Cell.CellDescr.FormatString))
                    //{
                    //    // Проверяем чтобы в качестве разделителя был допутимый символ (чтобы значение можно было конвертировать).
                    //    String modif = Cell.ModifiedValue;
                    //    modif = modif.Replace(".", System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                    //    modif = modif.Replace(",", System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);

                    //    text = modif;
                    //    try
                    //    {
                    //        double value = Convert.ToDouble(modif);
                    //        String str = value.ToString(Cell.CellDescr.FormatString);
                    //        if (str != Cell.CellDescr.FormatString) // Для случаев Currency и т.д.
                    //            text = str;
                    //    }
                    //    catch
                    //    {
                    //    }
                    //}
                    m_Caption_Text.Text = Cell.ModifiedValue;
                }
                else
                {
                    if (Cell.CellDescr != null && Cell.CellDescr.Value != null)
                    {
                        m_Caption_Text.Text = Cell.CellDescr.Value.DisplayValue;
                    }
                    else
                    {
                        m_Caption_Text.Text = String.Empty;
                    }
                }

                if (m_CustomCellAppearance != null && m_CustomCellAppearance.Options.UseForeColor)
                {
                    // Цвет из настроек условий для ячейки
                    m_Caption_Text.Foreground = new SolidColorBrush(m_CustomCellAppearance.ForeColor);
                }
                else
                {
                    Brush brush = new SolidColorBrush(Colors.Black);
                    if (!IsModified && Cell.CellDescr != null)
                    {
                        int foreColor = Cell.CellDescr.ForeColor;
                        if (foreColor != int.MinValue)
                        {
                            // Цвет из OLAP (свойства ячейки)
                            Color color = ColorHelper.FromRgb(foreColor);
                            brush = new SolidColorBrush(color);
                        }
                    }
                    m_Caption_Text.Foreground = brush;
                }
            }
            else
            {
                m_Caption_Text.Visibility = Visibility.Collapsed;
            }

            // Картинка из условия
            if (show_Image)
            {
                if (m_Image == null)
                {
                    m_Image = new Image();
                    m_Image.Width = m_Image.Height = 16;
                    m_Image.Visibility = Visibility.Collapsed;
                    m_Image.Stretch = Stretch.Uniform;
                    m_LayoutPanel.Children.Add(m_Image);
                    Grid.SetColumn(m_Image, 0);
                }

                m_Image.Visibility = Visibility.Visible;
                m_Image.Source = m_CustomCellAppearance.CustomImage;
                if(m_CustomCellAppearance.CustomImage != null)
                {
                    m_Image.Width = m_CustomCellAppearance.CustomImage.PixelWidth * Scale;
                    m_Image.Height = m_CustomCellAppearance.CustomImage.PixelHeight * Scale;
                }
                if (show_Value)
                {
                    // Отображается картинка и значение        
                    Grid.SetColumnSpan(m_Image, 1);
                }
                else
                {
                    // Отображается только картинка
                    Grid.SetColumnSpan(m_Image, 3);
                }
            }
            else
            {
                if (m_Image != null)
                {
                    m_Image.Visibility = Visibility.Collapsed;
                }
            }
        }

        //void ApplyHintSettings()
        //{
        //    // Подсказка
        //    if (Owner.Cells_UseHint)
        //    {
        //        if (Cell != null)
        //        {
        //            // Собственный контрол для отображения подсказки
        //            m_ToolTip = new ToolTipControl();
        //            if (Cell.CellDescr != null && Cell.CellDescr.Value != null && Cell.CellDescr.Value.Value != null)
        //            {
        //                m_ToolTip.Caption = Cell.CellDescr.Value.DisplayValue;
        //            }
        //            else
        //            {
        //                m_ToolTip.Caption = "(null)";
        //            }
        //            m_ToolTip.Text = Cell.GetShortTupleToStr();
        //            ToolTipService.SetToolTip(this, m_ToolTip);
        //        }
        //        else
        //        {
        //            ToolTipService.SetToolTip(this, null);
        //        }
        //    }
        //}


        bool m_IsSelected = false;
        public bool IsSelected
        {
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value;
                    ApplyBackgroundSettings();
                }
            }
            get
            {
                return m_IsSelected;
            }
        }

        public bool IsModified
        {
            set
            {
                if (Cell != null)
                    Cell.IsModified = value;
                ApplyBackgroundSettings();
            }
            get
            {
                if (Cell != null)
                    return Cell.IsModified;
                return false;
            }
        }

        bool m_IsFocused = false;
        public bool IsFocused
        {
            set
            {
                if (m_IsFocused != value)
                {
                    m_IsFocused = value;
                    ApplyBorderSettings();
                    ApplyBackgroundSettings();
                }
            }
            get
            {
                return m_IsFocused;
            }
        }

        bool m_IsEditing = false;
        public bool IsEditing
        {
            set
            {
                m_IsEditing = value;
            }
            get
            {
                return m_IsEditing;
            }
        }
    }
}