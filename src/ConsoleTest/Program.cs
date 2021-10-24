using System;

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

        [MakeProperty("Name", true, false)]
        private string? _name;

        [MakeProperty("Description", false, false)]
        private string? _description;

        public static void Main(string[] args)
        {
            new Program().Start();
        }

        private void Start()
        {
            Status = 1;
            Status = 1;
            Status = 2;
            Name = "Raf";
            Name = "Raf";
            Name = "Hello";
        }

        partial void OnStatusChanged(int oldValue, int newValue)
        {
            Console.WriteLine($"{oldValue} -> {newValue}");
        }

        partial void OnNameChanged(string? oldValue, string? newValue)
        {
            Console.WriteLine($"{oldValue} -> {newValue}");
        }
    }



}
