using System;

namespace SQLite.WinRT
{
    [AttributeUsage (AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute (string name)
        {
            Name = name;
        }
    }
}