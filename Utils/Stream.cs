using System.Collections.Immutable;
using NUnit.Framework;

namespace Translator.Utils;

public class Stream<T>
{
    public Stream(IEnumerable<T> data)
    {
        Data = data.ToImmutableList();
    }
    
    private Stream(ImmutableList<T> data, int index)
    {
        Data = data;
        Index = index;
    }

    private ImmutableList<T> Data { get; }
    private int Index { get; set; }
    
    public T Next() => Data[Index++];
    
    public bool HasNext() => Index < Data.Count;
    
    public T LookAhead() => Data[Index];
    
    public Stream<T> Fork() => new(Data, Index);
    
    public IEnumerable<T> TakeWhile(Predicate<T> predicate)
    {
        while (true)
        {
            if (!HasNext())
                throw new Exception("Stream has no more elements");

            if (!predicate(LookAhead()))
                yield break;
            yield return Next();
        }
    }
}


internal class StreamTests
{
    [Test]
    public void Stream_TestNext_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestLookAhead_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        Assert.AreEqual(1, stream.LookAhead());
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(2, stream.LookAhead());
        Assert.AreEqual(2, stream.LookAhead());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(3, stream.LookAhead());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(4, stream.LookAhead());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.LookAhead());
        Assert.AreEqual(5, stream.LookAhead());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestFork_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        var fork = stream.Fork();
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(1, fork.Next());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(2, fork.Next());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(3, fork.Next());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(4, fork.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.AreEqual(5, fork.Next());
        Assert.IsFalse(stream.HasNext());
        Assert.IsFalse(fork.HasNext());
    }
    
    [Test]
    public void Stream_TestTakeWhile_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        var result = stream.TakeWhile(x => x < 4).ToList();
        Assert.AreEqual(new[] {1, 2, 3}, result);
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestTakeWhile_ThrowsWhenStops()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Assert.Throws<Exception>(() => stream.TakeWhile(x => x < 10).ToArray());
    }
}
