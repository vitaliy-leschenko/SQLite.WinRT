using System;

namespace SQLite.WinRT.Tests.Data
{
    [Table("Items")]
    public class Item
    {
        [PrimaryKey, AutoIncrement]
        public int ItemID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; }

        public int? Data { get; set; }

        public DateTime? Time { get; set; }

        public bool Boolean { get; set; }
    }
}