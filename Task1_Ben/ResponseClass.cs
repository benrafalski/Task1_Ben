using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Task1_Ben
{
    public class ResponseClass
    {
        public Root rootObject;
        public ResponseClass(string json)
        {
            rootObject = JsonConvert.DeserializeObject<Root>(json);
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Argument
        {
            public List<Variable> variables { get; set; }
        }

        public class Indicator
        {
            public string code { get; set; }
            public int count { get; set; }
            public double share { get; set; }
        }


        public class Browser
        {
            public string code { get; set; }
            public int count { get; set; }
            public double share { get; set; }
        }

        public class Country
        {
            public string code { get; set; }
            public int count { get; set; }
            public double share { get; set; }
        }

        public class Lang
        {
            public string code { get; set; }
            public int count { get; set; }
            public double share { get; set; }
        }

        public class Oss
        {
            public string code { get; set; }
            public int count { get; set; }
            public double share { get; set; }
        }

        public class Payload
        {
            public Argument argument { get; set; }
            public Values values { get; set; }
        }

        public class Range
        {
            public string dataTypeName { get; set; }
            public string rangeTypeName { get; set; }
            public List<string> values { get; set; }
        }

        public class Root
        {
            public int ver { get; set; }
            public int statusCode { get; set; }
            public string payloadTypeName { get; set; }
            public List<Payload> payload { get; set; }
        }

        public class Values
        {
            public List<Indicator> countries { get; set; }
            public List<Indicator> langs { get; set; }
            public List<Indicator> oss { get; set; }
            public List<Indicator> browsers { get; set; }
            public double goodness { get; set; }
            public double badness { get; set; }
            public double painFactor { get; set; }
        }

        public class Variable
        {
            public string varName { get; set; }
            public Range range { get; set; }
        }


    }
}
