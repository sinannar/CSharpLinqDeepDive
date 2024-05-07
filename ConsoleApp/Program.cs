
//IEnumerable<Person> people = new List<Person>()
//{
//    new Person { Name = "Scott"},
//    new Person { Name = "Alice"},
//    new Person { Name = "Bob"}
//};

//IEnumerable<string> names = people.Select(p => p.Name);

//class Person
//{
//    public string Name { get; set; }
//}


IEnumerable<int> source = new List<int>() { 1, 2, 3 };
foreach (var i in Select(source, i => i * 2))
{
    Console.WriteLine(i);
}

static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    foreach (var item in source)
    {
        yield return selector(item);
    }
}
