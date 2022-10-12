namespace Rust2SharpTranslator.Utils;

public static class Extensions
{
    public static T Unwrap<T>(this T? value) where T : struct
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return value.Value;
    }

    public static T Unwrap<T>(this T? value) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return value;
    }
}
