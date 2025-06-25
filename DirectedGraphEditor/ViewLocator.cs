using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace DirectedGraphEditor
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type == null)
            {
                return new TextBlock { Text = "Not Found: " + name };
            }

            return (Control)Activator.CreateInstance(type)!;
        }

        public bool Match(object? data) => data is not null && data.GetType().Name.EndsWith("ViewModel");

    }
}