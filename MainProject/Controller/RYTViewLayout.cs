//
//  RYTViewLayout
//
//  Created by wu.dong on 9/16/11.
//  Copyright 2011 RYTong. All rights reserved.
//

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RYTong.ControlLib;

namespace RYTong.MainProject.Controller
{
    public class RYTViewLayout
    {
        #region  属性

        public double xPadding;
        public double yPadding;
        public double horSpacing;
        public double verSpacing;

        #endregion

        public Size layoutRootControls(List<RYTControl> rootControls, Panel view)
        {
            double x = xPadding;
            double y = yPadding;

            for (int i = 0; i < rootControls.Count; i++)
            {
                rootControls[i].Measure(new Size(rootControls[i].Frame_.Width, rootControls[i].Frame_.Height));
                rootControls[i].LayoutSubviews();

                if (double.IsNaN(rootControls[i].View_.Width))
                {
                    rootControls[i].View_.UpdateLayout();
                    rootControls[i].View_.Width = rootControls[i].View_.ActualWidth;
                    rootControls[i].Frame_.Width = rootControls[i].View_.Width;
                }

                if (((x + rootControls[i].Frame_.Width > view.Width && x != xPadding) || rootControls[i] is RYTBrControl) && i > 0)
                {
                    //换行.
                    x = xPadding;
                    y = y + rootControls[i - 1].Frame_.Height + verSpacing;
                }
                 
                // Set Frame_'s width and height from view's width and height
                if (rootControls[i].View_ != null)
                {
                    rootControls[i].Frame_ = new Rect(x, y, rootControls[i].View_.Width, rootControls[i].View_.Height);

                    if (rootControls[i].Frame_.Width != 0)
                    {
                        x = x + rootControls[i].Frame_.Width + horSpacing;
                    }
                }
            }

            return new Size(0.0, 0.0);
        }
    }
}
