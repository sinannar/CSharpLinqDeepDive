using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections;
using System.Diagnostics;

//var ssss = Enumerable.Range(0, 1000);
//IEnumerable<int> xxxx = from i in ssss
//                        where i % 2 == 0
//                        select i * 2;

IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();
Console.WriteLine(Enumerable.Select(Enumerable.Where(source, i => i % 2 == 0), i => i * 2).Sum());
Console.WriteLine(Tests.SelectCompiler(Tests.WhereCompiler(source, i => i % 2 == 0), i => i * 2).Sum());
Console.WriteLine(Tests.SelectManual(Tests.WhereManual(source, i => i % 2 == 0), i => i * 2).Sum());

//for (int i = 0; i < 10_000; ++i)
//{
//    foreach (int value in SelectManual(source, x => x * 2)) { }
//}

//Console.WriteLine(Enumerable.Select(source, x => x * 2).Sum());

//var c = SelectCompiler(source, x => x * 2);
//Console.WriteLine(c.Sum());
//Console.WriteLine(c.Sum());

//var m = SelectManual(source, x => x * 2);
//Console.WriteLine(m.Sum());
//Console.WriteLine(m.Sum());

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[ShortRunJob]
public class Tests
{
    private IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();

    [Benchmark]
    public int SumCompiler()
    {
        int sum = 0;
        foreach (int i in SelectCompiler(source, i => i * 2))
        {
            sum += i;
        }
        return sum;
    }


    [Benchmark]
    public int SumManual()
    {
        int sum = 0;
        foreach (int i in SelectManual(source, i => i * 2))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int SumLinq()
    {
        int sum = 0;
        foreach (int i in Enumerable.Select(Enumerable.Where(source, i => i % 2 == 0), i => i * 2))
        {
            sum += i;
        }
        return sum;
    }

    public static IEnumerable<TResult> SelectCompiler<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (source is TSource[] array)
        {
            return ArrayImpl(array, selector);
        }
        return EnumerableImpl(source, selector);

        static IEnumerable<TResult> EnumerableImpl<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {

            foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        static IEnumerable<TResult> ArrayImpl<TSource, TResult>(TSource[] source, Func<TSource, TResult> selector)
        {
            for (var i = 0; i < source.Length; i++)
            {
                yield return selector(source[i]);
            }
        }
    }

    public static IEnumerable<TSource> WhereCompiler<TSource>(IEnumerable<TSource> source, Func<TSource, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        //if (source is TSource[] array)
        //{
        //    return ArrayImpl(array, selector);
        //}
        return EnumerableImpl(source, filter);

        static IEnumerable<TSource> EnumerableImpl(IEnumerable<TSource> source, Func<TSource, bool> filter)
        {

            foreach (var item in source)
            {
                if (filter(item))
                {
                    yield return item;
                }
            }
        }

        //static IEnumerable<TResult> ArrayImpl<TSource, TResult>(TSource[] source, Func<TSource, TResult> selector)
        //{
        //    for (var i = 0; i < source.Length; i++)
        //    {
        //        yield return selector(source[i]);
        //    }
        //}
    }

    public static IEnumerable<TResult> SelectManual<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (source is TSource[] array)
        {
            return new SelectManualArray<TSource, TResult>(array, selector);
        }
        return new SelectManualEnumerable<TSource, TResult>(source, selector);
    }

    public static IEnumerable<TSource> WhereManual<TSource>(IEnumerable<TSource> source, Func<TSource, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);

        return new WhereManualEnumerable<TSource>(source, filter);
    }


    sealed class SelectManualEnumerable<TSource, TResult> : IEnumerable<TResult>, IEnumerator<TResult>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, TResult> _selector;

        private int _threadId = Environment.CurrentManagedThreadId;
        private TResult _current = default!;
        private IEnumerator<TSource>? _enumerator;
        private int _state = 0;

        public SelectManualEnumerable(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            _source = source;
            _selector = selector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            //if(Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            if (_threadId == Environment.CurrentManagedThreadId && _state == 0)
            {
                _state = 1;
                return this;
            }
            return new SelectManualEnumerable<TSource, TResult>(_source, _selector) { _state = 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            switch (_state)
            {
                case 1:
                    _enumerator = _source.GetEnumerator();
                    _state = 2;
                    goto case 2;
                case 2:
                    Debug.Assert(_enumerator is not null);
                    try
                    {
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                            // yield return
                        }
                    }
                    catch
                    {
                        Dispose();
                        throw;
                    }
                    break;
            }

            Dispose();
            return false;
        }

        public void Dispose()
        {
            _state = -1;
            _enumerator?.Dispose();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }

    sealed class WhereManualEnumerable<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, bool> _filter;

        private int _threadId = Environment.CurrentManagedThreadId;
        private TSource _current = default!;
        private IEnumerator<TSource>? _enumerator;
        private int _state = 0;

        public WhereManualEnumerable(IEnumerable<TSource> source, Func<TSource, bool> filter)
        {
            _source = source;
            _filter = filter;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            //if(Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            if (_threadId == Environment.CurrentManagedThreadId && _state == 0)
            {
                _state = 1;
                return this;
            }
            return new WhereManualEnumerable<TSource>(_source, _filter) { _state = 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TSource Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            switch (_state)
            {
                case 1:
                    _enumerator = _source.GetEnumerator();
                    _state = 2;
                    goto case 2;
                case 2:
                    Debug.Assert(_enumerator is not null);
                    try
                    {
                        while (_enumerator.MoveNext())
                        {
                            TSource current = _enumerator.Current;
                            if (_filter(current))
                            {
                                _current = current;
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        Dispose();
                        throw;
                    }
                    break;
            }

            Dispose();
            return false;
        }

        public void Dispose()
        {
            _state = -1;
            _enumerator?.Dispose();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }


    sealed class SelectManualArray<TSource, TResult> : IEnumerable<TResult>, IEnumerator<TResult>
    {
        private TSource[] _source;
        private Func<TSource, TResult> _selector;

        private int _threadId = Environment.CurrentManagedThreadId;
        private TResult _current = default!;
        private int _state = 0;

        public SelectManualArray(TSource[] source, Func<TSource, TResult> selector)
        {
            _source = source;
            _selector = selector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            //if(Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            if (_threadId == Environment.CurrentManagedThreadId && _state == 0)
            {
                _state = 1;
                return this;
            }
            return new SelectManualArray<TSource, TResult>(_source, _selector) { _state = 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            int i = _state - 1;
            TSource[] source = _source;


            if ((uint)i < (uint)source.Length)
            {
                _state++;
                _current = _selector(source[i]);
                return true;
            }


            Dispose();
            return false;
        }

        public void Dispose()
        {
            _state = -1;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }

}