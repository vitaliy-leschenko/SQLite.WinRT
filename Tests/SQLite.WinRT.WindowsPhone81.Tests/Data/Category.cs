namespace SQLite.WinRT.Tests.Data
{
    [Table("Categories")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryID { get; set; }
        public string Name { get; set; }

        public byte[] Text { get; set; }
    }
}