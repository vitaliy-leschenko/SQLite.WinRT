using System;

namespace SQLite.WinRT
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DatabaseUpdateAttribute: Attribute
    {
        public Type UpdateType { get; private set; }

        public DatabaseUpdateAttribute(Type updateType)
        {
            UpdateType = updateType;
        }
    }
}