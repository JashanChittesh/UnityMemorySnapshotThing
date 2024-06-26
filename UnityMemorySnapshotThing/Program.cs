﻿using System.Text;
using UMS.Analysis;
using UMS.Analysis.Structures.Objects;

namespace UnityMemorySnapshotThing;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No file specified");
            return;
        }
        
        var filePath = args[0];

        var start = DateTime.Now;
        Console.WriteLine();
        Console.WriteLine("Reading snapshot file...");
        using var file = new SnapshotFile(filePath);
        Console.WriteLine($"Read snapshot file in {(DateTime.Now - start).TotalMilliseconds} ms\n");
        
        Console.WriteLine($"Snapshot file version: {file.SnapshotFormatVersion}\n");
        Console.WriteLine($"Snapshot taken on {file.CaptureDateTime}\n");
        Console.WriteLine($"Target platform: {file.ProfileTargetInfo}\n");
        Console.WriteLine($"Memory stats: {file.ProfileTargetMemoryStats}\n");
        Console.WriteLine($"VM info: {file.VirtualMachineInformation}\n");
        
        // Console.WriteLine("Querying large dynamic arrays...");
        // start = DateTime.Now;
        // Console.WriteLine($"Snapshot contains {file.NativeObjectNames.Length} native objects and {file.TypeDescriptionNames.Length} managed objects");
        //
        // var heapSections = file.ManagedHeapSectionBytes;
        // var heapSectionStartAddresses = file.ManagedHeapSectionStartAddresses;
        // Console.WriteLine($"Snapshot contains {heapSections.Length} managed heap sections (starting at {heapSectionStartAddresses.Length} start addresses) totalling {heapSections.Sum(b => b.Length)} bytes");
        //
        // var fieldIndices = file.TypeDescriptionFieldIndices;
        // var fieldBytes = file.TypeDescriptionStaticFieldBytes;
        // Console.WriteLine($"Snapshot contains {fieldIndices.Length} type description-field index mappings, totalling {fieldIndices.Sum(i => i.Length)} field indices, and {fieldBytes.Length} type description-static field bytes");
        //
        // var fieldNames = file.FieldDescriptionNames;
        // Console.WriteLine($"Snapshot contains {fieldNames.Length} field names");
        //
        // var fieldOffsets = file.FieldDescriptionOffsets;
        // Console.WriteLine($"Snapshot contains {fieldOffsets.Length} field offsets");
        //
        // var fieldTypes = file.FieldDescriptionTypeIndices;
        // Console.WriteLine($"Snapshot contains {fieldTypes.Length} field-type mappings");
        //
        // //Field indices map type description names to field names
        // //e.g. field indices element 2 has some values, so those values are the indices into the field name array for type description name 2
        //
        // Console.WriteLine($"Querying large dynamic arrays took {(DateTime.Now - start).TotalMilliseconds} ms\n");

        file.LoadManagedObjectsFromGcRoots();
        file.LoadManagedObjectsFromStaticFields();
        
        FindLeakedUnityObjects(file);
    }
    
    private static void FindLeakedUnityObjects(SnapshotFile file)
    {
        var start = DateTime.Now;
        Console.WriteLine("Finding leaked Unity objects...");
        
        //Find all the managed objects, filter to those which have a m_CachedObjectPtr field
        //Then filter to those for which that field is 0 (i.e. not pointing to a native object)
        //That gives the leaked managed shells.
        var ret = new StringBuilder();
        var str = $"Snapshot contains {file.AllManagedClassInstances.Count()} managed objects";
        Console.WriteLine(str);
        ret.AppendLine(str);

        var filterStart = DateTime.Now;

        var unityEngineObjects = file.AllManagedClassInstances.Where(i => i.InheritsFromUnityEngineObject(file)).ToArray();

        str = $"Of those, {unityEngineObjects.Length} inherit from UnityEngine.Object (filtered in {(DateTime.Now - filterStart).TotalMilliseconds} ms)";
        Console.WriteLine(str);
        ret.AppendLine(str);
        
        var detectStart = DateTime.Now;

        var leakSummary = new LeakSummary();

        foreach (var managedClassInstance in unityEngineObjects)
        {
            if (managedClassInstance.IsLeakedManagedShell(file))
            {
                var typeName = file.GetTypeName(managedClassInstance.TypeInfo.TypeIndex);

                str = $"Found leaked managed object of type: {typeName} at memory address 0x{managedClassInstance.ObjectAddress:X}";
                Console.WriteLine(str);
                ret.AppendLine(str);

                str = $"    Retention Path: {managedClassInstance.GetFirstObservedRetentionPath(file, leakSummary)}";
                Console.WriteLine(str);
                Console.WriteLine();
                ret.AppendLine(str).AppendLine();

                leakSummary.IncrementLeaked(typeName);
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        ret.AppendLine().AppendLine();

        AppendToLog(ret, $"Finished detection in {(DateTime.Now - detectStart).TotalMilliseconds} ms. {leakSummary.NumLeaked} of those are leaked managed shells");

        AppendToLog(ret, leakSummary.GetLeakedTypesSorted());
        AppendToLog(ret, leakSummary.GetLeakingUnityObjectsSorted());
        AppendToLog(ret, leakSummary.GetLeakingLeakingShellsSorted());

        File.WriteAllText("leaked_objects.txt", ret.ToString());
    }

    private static void AppendToLog(StringBuilder log, string str) {
        Console.WriteLine(str);
        Console.WriteLine();
        log.AppendLine(str).AppendLine();
    }
}