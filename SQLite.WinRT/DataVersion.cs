namespace SQLite.WinRT
{
    public class DataVersion
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int Value { get; set; }
    }
}
