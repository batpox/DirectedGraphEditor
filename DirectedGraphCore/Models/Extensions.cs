public static class DictionaryExtensions
{
    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        foreach (var kv in items) dict[kv.Key] = kv.Value;
    }
}