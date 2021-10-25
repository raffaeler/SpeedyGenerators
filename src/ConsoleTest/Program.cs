using System;
using System.Collections.ObjectModel;

using SpeedyGenerators;
namespace ConsoleTest
{
    public partial class Program
    {
        /// <summary>
        /// This is the status
        /// </summary>
        [MakeProperty("Status", true, true)]
        private int _status;

        //[MakeProperty("Name", true, false)]
        //private string? _name;

        //[MakeProperty("Description", false, false)]
        //private string? _description;

        //[MakeProperty("TextList", false, false)]
        //private ObservableCollection<string>? _textList;

        //[MakeProperty("X")]
        //private int _x;

        public static void Main(string[] args)
        {
            Test.Run();
            new Program().Start();
        }

        private void Start()
        {
            //Status = 0;
            //Status = 1;
            //Status = 1;
            //Status = 2;
            //Name = "Raf";
            //Name = "Raf";
            //Name = "Hello";
        }

        //partial void OnStatusChanged(int oldValue, int newValue)
        //{
        //    Console.WriteLine($"{oldValue} -> {newValue}");
        //}

        //partial void OnNameChanged(string? oldValue, string? newValue)
        //{
        //    Console.WriteLine($"{oldValue} -> {newValue}");
        //}
    }



}
