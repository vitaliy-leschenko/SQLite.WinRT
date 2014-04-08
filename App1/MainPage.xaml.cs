using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using SQLite.WinRT;

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void OnTestClick(object sender, RoutedEventArgs e)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var connection = new SQLiteAsyncConnection(Path.Combine(folder.Path, "test.db"));
            await connection.CreateTableAsync<TestEntity>();

            var item = new TestEntity();
            item.Value = new Random().Next();

            await connection.InsertAsync(item);
        }
    }

    [Table("TestEntity")]
    public class TestEntity
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public int Value { get; set; }
    }
}
