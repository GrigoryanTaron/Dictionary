using System;
using System.Collections.Generic;
using static System.Collections.Generic.Dictionary<string, string>;

namespace Dictionary
{
    internal class Program
    {
        static void Main(string[] args)
        {
            _Dictionary d = new _Dictionary();
            d.Add("Hello", "World");
            d.Add("How Much", "Is The Fish");
            d.Add("No Woman", "No Cry");
            d.Add("Don't Worry", "Be Happy");
            foreach (var item in d)
            {
                Console.WriteLine($"{item}");
            }
           bool c = d.ContainsKey("Hello");
            d.Remove("Hello");
            foreach (var item in d)
            {
                Console.WriteLine($"{item}");
            }
            d.Clear();
            foreach (var item in d)
            {
                Console.WriteLine($"{item}");
            }
            _Dictionary d1 = new _Dictionary(10);
            _Dictionary d2 = new _Dictionary(EqualityComparer<string>.Default);
            _Dictionary d3 = new _Dictionary(15, EqualityComparer<string>.Default);
            _Dictionary d4 = new _Dictionary(new Dictionary<string, string> { { "get up", "stund up" }, { "sun", "is shining" } });
            _Dictionary d5 = new _Dictionary(new Dictionary<string, string> { { "Johnny was", "a good man" }, { "I Shot", "the Sheriff" } }, EqualityComparer<string>.Default);
        }
    }
}
