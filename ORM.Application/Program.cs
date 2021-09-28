using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ORM.Application.Entities;
using ORM.Core.Database;
using ORM.LinqToSql;

namespace ORM.Application
{
    public class Person
    {
        public string Name { get; set; }

        public string Gender { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var json = JsonConvert.SerializeObject(this);
            var obj = JsonConvert.DeserializeObject(json) as JObject;
            return obj.ToObject<Dictionary<string, object>>();
        }

        public static Person FromDictionary(Dictionary<string, object> dictionary)
        {
            string json = JsonConvert.SerializeObject(dictionary);
            var person = JsonConvert.DeserializeObject<Person>(json);
            return person;
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is not Person person)
            {
                return base.Equals(obj);

            }

            return Name == person.Name && Gender == person.Gender;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var translator = new QueryTranslator();
            var provider = new SqlQueryProvider(translator);
            var dbSet = new DbSet<Book>(provider);

            //var r = dbSet.Where(x => x.Title == "Sam").ToList();
            var r = dbSet.Where(x => x.Price > 0).OrderBy(x => x.Likes).ToList();
        }
    }
}