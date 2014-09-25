namespace SQLite.WinRT.Tests.Data
{
    [Table("Countries")]
    public class Country
    {
        [PrimaryKey]
        public string CountryCode { get; set; }
        public string Text { get; set; }
    }
}