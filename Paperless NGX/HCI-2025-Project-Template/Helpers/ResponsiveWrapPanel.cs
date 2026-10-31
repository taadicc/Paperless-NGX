using System;
using System.Windows;
using System.Windows.Controls;

namespace HCI_2025_Project_Template.Helpers
{
    /// <summary>
    /// Panel koji automatski dijeli dostupnu širinu na jednake kolone,
    /// minimalna širina kartice je MinItemWidth.
    /// </summary>
    public class ResponsiveWrapPanel : Panel
    {
        public static readonly DependencyProperty MinItemWidthProperty =
            DependencyProperty.Register(nameof(MinItemWidth), typeof(double),
                typeof(ResponsiveWrapPanel),
                new FrameworkPropertyMetadata(220.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ItemSpacingProperty =
            DependencyProperty.Register(nameof(ItemSpacing), typeof(double),
                typeof(ResponsiveWrapPanel),
                new FrameworkPropertyMetadata(16.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double MinItemWidth
        {
            get => (double)GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        public double ItemSpacing
        {
            get => (double)GetValue(ItemSpacingProperty);
            set => SetValue(ItemSpacingProperty, value);
        }

        private int _columns = 1;
        private double _itemWidth = 220;

        protected override Size MeasureOverride(Size availableSize)
        {
            double panelWidth = double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width;
            double spacing = ItemSpacing;
            double minW = MinItemWidth;

            // Koliko kolona stane?
            _columns = Math.Max(1, (int)((panelWidth + spacing) / (minW + spacing)));
            _itemWidth = (panelWidth - spacing * (_columns - 1)) / _columns;

            double totalHeight = 0;
            int col = 0;
            double rowHeight = 0;

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(_itemWidth, availableSize.Height));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
                col++;
                if (col >= _columns)
                {
                    totalHeight += rowHeight + spacing;
                    rowHeight = 0;
                    col = 0;
                }
            }
            if (col > 0) totalHeight += rowHeight;

            return new Size(panelWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double spacing = ItemSpacing;
            double panelWidth = finalSize.Width;

            _columns = Math.Max(1, (int)((panelWidth + spacing) / (MinItemWidth + spacing)));
            _itemWidth = (panelWidth - spacing * (_columns - 1)) / _columns;

            int col = 0;
            double x = 0, y = 0;
            double rowHeight = 0;

            // Prvo izmjeri sve u redu da bismo znali visinu reda
            int totalChildren = InternalChildren.Count;
            int i = 0;

            while (i < totalChildren)
            {
                // Izračunaj visinu reda
                rowHeight = 0;
                for (int c = 0; c < _columns && (i + c) < totalChildren; c++)
                {
                    rowHeight = Math.Max(rowHeight, InternalChildren[i + c].DesiredSize.Height);
                }

                // Postavi djecu u red
                x = 0;
                for (col = 0; col < _columns && i < totalChildren; col++, i++)
                {
                    var child = InternalChildren[i];
                    child.Arrange(new Rect(x, y, _itemWidth, rowHeight));
                    x += _itemWidth + spacing;
                }

                y += rowHeight + spacing;
            }

            return finalSize;
        }
    }
}