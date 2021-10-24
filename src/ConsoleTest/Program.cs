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

        public static void Main(string[] args)
        {
            new Program().Start();
        }

        private void Start()
        {
            Status = 1;
            Status = 2;
        }

        partial void OnStatusChanged(int oldValue, int newValue)
        {
            Console.WriteLine($"{oldValue} -> {newValue}");
        }
    }



}
