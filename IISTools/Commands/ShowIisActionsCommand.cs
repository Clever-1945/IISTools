using IISTools.Popups;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace IISTools.Commands
{
    
    public class ShowIisActionsCommand: IISToolsCommandBase<ShowIisActionsCommand>
    {
        public class ButtonUnderMouse 
        {
            /// <summary> Кнопка </summary>
            public Button Buttun;
            /// <summary> Позиционирование кнопки </summary>
            public Point ButtonTopLeft;
            /// <summary> Координаты курсора мышки </summary>
            public System.Drawing.Point MousePoint;
        }

        private System.Windows.Controls.Primitives.Popup _activePopup;

        public override void Execute(object sender, EventArgs e)
        {
            if (_activePopup != null)
            {
                _activePopup.IsOpen = false;
                _activePopup = null;
            }
            
            var buttonUnderMouse = GetButtonUnderMouse();
            if (buttonUnderMouse == null)
                return;

            var offset = GetPopupOffset(buttonUnderMouse);

            _activePopup = new System.Windows.Controls.Primitives.Popup();
            var converter = new BrushConverter();

            // 2. Помещаем внутрь ЛЮБОЙ ваш кастомный WPF-контрол
            var control = new IisActionsPopup();
            _activePopup.Child = control;

            // 3. Настраиваем поведение (закрытие при клике мимо)
            _activePopup.StaysOpen = true;
            _activePopup.AllowsTransparency = true;

            // 4. Позиционируем под курсором или кнопкой тулбара
            _activePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;

            // 5. Открываем
            _activePopup.IsOpen = true;
            _activePopup.HorizontalOffset = offset.X;
            _activePopup.VerticalOffset = offset.Y;
            _activePopup.Closed += (object sender, EventArgs e) =>
            {
                var popup = sender as Popup;
                if (popup != null)
                {
                    if (popup == _activePopup)
                    {
                        _activePopup.IsOpen = false;
                        _activePopup = null;
                    }
                }
            };
        }

        /// <summary>
        /// Получить смещение панели
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        private Point GetPopupOffset(ButtonUnderMouse button)
        {
            var x = button.ButtonTopLeft.X - button.MousePoint.X;
            var y = (button.ButtonTopLeft.Y + button.Buttun.ActualHeight) - button.MousePoint.Y;

            return new Point(x, y);
        }

        /// <summary>
        /// Находит кнопку под курсором мышки
        /// </summary>
        /// <returns></returns>
        private ButtonUnderMouse GetButtonUnderMouse()
        {
            var mousePoint = System.Windows.Forms.Cursor.Position;
            Window mainWindow = Application.Current.MainWindow;

            ButtonUnderMouse buttonUnderMouse = null;
            ReadControls(mainWindow, (child) =>
            {
                var button = child as Button;
                if (button == null)
                    return true;    // продолжаем читать

                GeneralTransform transform = button.TransformToAncestor(mainWindow);
                Point buttonTopLeft = transform.Transform(new Point(0, 0));

                if (buttonTopLeft.X < mousePoint.X && buttonTopLeft.Y < mousePoint.Y)
                {
                    if ((button.ActualWidth + buttonTopLeft.X) > mousePoint.X)
                    {
                        if ((button.ActualHeight + buttonTopLeft.Y) > mousePoint.Y)
                        {
                            buttonUnderMouse = new ButtonUnderMouse() 
                            {
                                Buttun = button,
                                ButtonTopLeft = buttonTopLeft,
                                MousePoint = mousePoint
                            };
                            return false;       // закончили читать
                        }
                    }
                }

                return true;
            });

            return buttonUnderMouse;
        }

        /// <summary>
        /// Прочитать всех детей у родителя
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="canContinue"></param>
        /// <returns> false если не все дети были вычитаны </returns>
        private bool ReadControls(DependencyObject parent, Func<DependencyObject, bool> canContinue)
        {
            if (parent == null)
                return false;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!canContinue(child))
                    return false;

                if (!ReadControls(child, canContinue))
                    return false;
            }

            return true;
        }
    }
}
