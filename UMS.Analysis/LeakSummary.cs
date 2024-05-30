namespace UMS.Analysis;

public class LeakSummary 
{
    public int NumLeaked { get; private set; } = 0;

    private readonly Dictionary<string, int> _leakedTypes = new();

    private readonly Dictionary<string, int> _leakingParentsUnityObjects = new();

    private readonly Dictionary<string, int> _leakingParentsShells = new();

    public void IncrementLeaked(string typeName) {
        _leakedTypes[typeName] = _leakedTypes.GetValueOrDefault(typeName) + 1;
        NumLeaked++;
    }

    public void RegisterLeakingUnityObject(string retentionReason) 
    {
        if (_leakingParentsUnityObjects.ContainsKey(retentionReason)) 
        {
            _leakingParentsUnityObjects[retentionReason]++;
        }
        else 
        {
            _leakingParentsUnityObjects.Add(retentionReason, 1);
        }
    }

    public void RegisterLeakingShell(string retentionReason) 
    {
        if (_leakingParentsShells.ContainsKey(retentionReason)) 
        {
            _leakingParentsShells[retentionReason]++;
        } 
        else 
        {
            _leakingParentsShells.Add(retentionReason, 1);
        }
    }

    public string GetLeakedTypesSorted() 
    {
        return $"Leaked types by count: \n{DictionaryToSortedList(_leakedTypes)}";
    }

    public string GetLeakingUnityObjectsSorted() {
        return $"Leaking game objects by count: \n{DictionaryToSortedList(_leakingParentsUnityObjects)}";
    }

    public string GetLeakingLeakingShellsSorted() {
        return $"Leaking shells by count: \n{DictionaryToSortedList(_leakingParentsShells)}";
    }

    private string DictionaryToSortedList(Dictionary<string, int> dictionary) 
    {
        var leakedTypesSorted = dictionary.OrderByDescending(kvp => kvp.Value).ToArray();

        return string.Join("\n", leakedTypesSorted.Select(kvp => $"{kvp.Value} x {kvp.Key}"));
    }

}
