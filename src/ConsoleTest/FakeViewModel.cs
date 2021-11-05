using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using SpeedyGenerators;

namespace ConsoleTest
{
    public partial class FakeViewModel
    {
        /// <summary>
        /// My Status
        /// </summary>
        [MakeProperty("Status")]
        private int _status;

        [MakeProperty("Name", true)]
        private string? _name;

        [MakeProperty("Description", true, true)]
        private string? _description;

        [MakeProperty("FirstName", true)]
        private string? _firstName;

        [MakeProperty("LastName", true)]
        private string? _lastName;

        partial void OnOnePropertyHasChanged(string propertyName)
        {
            Console.WriteLine(propertyName);
        }

        [MakeProperty("ViewModels")]
        private ObservableCollection<FakeViewModel> _viewModels = new();

        partial void OnDescriptionChanged(string? oldValue, string? newValue)
        {
            Console.WriteLine($"{oldValue} -> {newValue}");
        }

        partial void OnNameChanged(string? oldValue, string? newValue)
        {
            Console.WriteLine($"{oldValue} -> {newValue}");
        }
    }


    public class Test
    {
        public static void Run()
        {
            var foo = new FakeViewModel();
            foo.Status = 1;
            foo.Status = 1;
            foo.Status = 2;

            foo.Name = "Raf";
            foo.Name = "Raf";
            foo.Name = "Dan";


            foo.Description = "hello";
            foo.Description = "hello";
            foo.Description = "world";
        }
    }

}
