using System;
using DirectedGraphEditor.Controls;

namespace DirectedGraphEditor.Services
{
    // Simple runtime settings holder. Expand to persistence when needed.
    public static class EditorSettings
    {
        //private static EdgeStyle _defaultEdgeStyle = EdgeStyle.Linear;
        private static EdgeStyle _defaultEdgeStyle = EdgeStyle.RoutedBezier;

        public static event Action<EdgeStyle>? EdgeStyleChanged;

        public static EdgeStyle DefaultEdgeStyle
        {
            get => _defaultEdgeStyle;
            set
            {
                if (_defaultEdgeStyle == value) return;
                _defaultEdgeStyle = value;
                EdgeStyleChanged?.Invoke(value);
            }
        }
    }
}