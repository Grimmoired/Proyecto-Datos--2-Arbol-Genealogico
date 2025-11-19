using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Globalization;

namespace Proyecto__2_Datos_Arbol_Genealogico.Models
{
    public class Person
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Cedula { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public double Latitude { get; set; }  
        public double Longitude { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateOfDeath { get; set; } = null;
        public BitmapImage Photo { get; set; } = null;

        public int Age
        {
            get
            {
                DateTime end = DateOfDeath ?? DateTime.Now;
                int age = end.Year - DateOfBirth.Year;
                if (end.Month < DateOfBirth.Month || (end.Month == DateOfBirth.Month && end.Day < DateOfBirth.Day))
                    age--;
                return age;
            }
        }

        public bool IsAlive => DateOfDeath == null;

        public string FullName => $"{FirstName} {LastName}".Trim();

        public Person() { }

        public static BitmapImage LoadImageFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(Path.GetFullPath(path));
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }
    }
}
