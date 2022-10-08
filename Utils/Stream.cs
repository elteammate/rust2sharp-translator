using System.Collections.Immutable;

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
