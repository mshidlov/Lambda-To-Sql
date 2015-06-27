using System;
using NUnit.Framework;
using Lambda_To_Sql;
namespace Tests
{
    class Example
    {
        [CustomColumn]
        public int IntExample { get; set; }
        [CustomColumn]
        public string StringExample { get; set; }
        [CustomColumn]
        public bool BoolExample { get; set; }
        [CustomColumn]
        public decimal DecimalExample { get; set; }
        [CustomColumn]
        public DateTime DateTimeExample { get; set; }
    }

    class JoinExample
    {
        [CustomColumn]
        public int IntExample { get; set; }
        [CustomColumn]
        public string StringExample { get; set; }
        [CustomColumn]
        public bool BoolExample { get; set; }
        [CustomColumn]
        public decimal DecimalExample { get; set; }
        [CustomColumn]
        public DateTime DateTimeExample { get; set; }
    }

    [TestFixture]
    public class QueryBuilderTests
    {
        [Test]
        public void test()
        {
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            var query =
                new QueryBuilder<Example>()
                .Join<JoinExample>(x=>x.IntExample,x=>x.DateTimeExample)
                .Where(
                    x =>
                        x.DateTimeExample > from && x.DateTimeExample < to &&
                        (x.StringExample == "Test" || x.BoolExample))
                    .GroupBy(x=> x.IntExample,x=> x.StringExample)
                    .OrderBy(x => x.StringExample)
                    .Sum(x => x.DecimalExample)
                    .Select();
        }
    }
}
