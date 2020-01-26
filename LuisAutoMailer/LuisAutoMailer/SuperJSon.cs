using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuisAutoMailer
{
    public class SuperJSon
    {
        public class TopScoringIntent
        {
            public string intent { get; set; }
            public double score { get; set; }
        }

        public class Intent
        {
            public string intent { get; set; }
            public double score { get; set; }
        }

        public class Resolution
        {
            public string subtype { get; set; }
            public string value { get; set; }
        }

        public class Entity
        {
            public string entity { get; set; }
            public string type { get; set; }
            public int startIndex { get; set; }
            public int endIndex { get; set; }
            public double score { get; set; }
            public Resolution resolution { get; set; }
        }

        public List<CompositeEntity> compositeEntities;
        
        public class RootObject
        {
            public string query { get; set; }
            public TopScoringIntent topScoringIntent { get; set; }
            public List<Intent> intents { get; set; }
            public List<Entity> entities { get; set; }
            public List<CompositeEntity> compositeEntities { get; set; }
        }


    }
    public class CompositeEntity
    {
        public string parentType { get; set; }
        public string value { get; set; }
        public List<Child> children { get; set; }
    }

    public class Child
    {
        public string type { get; set; }
        public string value { get; set; }
    }

}
