
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



static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    foreach (var item in source)
    {
        yield return selector(item);
    }
}
