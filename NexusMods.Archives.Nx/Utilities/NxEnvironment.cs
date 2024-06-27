using System.Runtime.InteropServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Platform specific utilities.
/// </summary>
internal class NxEnvironment
{
    public static int PhysicalCoreCount { get; } = GetPhysicalCoreCount();

    /// <summary>
    ///     Returns the physical core count on this machine (excluding hyperthreads).
    /// </summary>
    private static unsafe int GetPhysicalCoreCount()
    {
        #if NET5_0_OR_GREATER
        var isLinux = OperatingSystem.IsLinux(); // intrinsic/constant
        var isWindows = OperatingSystem.IsWindows(); // intrinsic/constant
        #else
        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        #endif

        if (isLinux)
        {
            using var file = new StreamReader("/proc/cpuinfo");
            while (file.ReadLine() is { } line)
            {
                if (!line.Contains("cpu cores"))
                    continue;

                return int.Parse(line.Split(':')[1].Trim());
            }

            return Environment.ProcessorCount;
        }

        if (isWindows)
        {
            uint returnLength = 0;
            GetLogicalProcessorInformation(IntPtr.Zero, ref returnLength);
            if (returnLength == 0)
                return Environment.ProcessorCount;

            var info = stackalloc SYSTEM_LOGICAL_PROCESSOR_INFORMATION[(int)returnLength / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION)];
            if (!GetLogicalProcessorInformation((IntPtr)info, ref returnLength))
                return Environment.ProcessorCount;

            var count = (int)returnLength / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION);
            var physicalCoreCount = 0;
            for (var x = 0; x < count; x++)
            {
                if (info[x].Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                    physicalCoreCount++;
            }

            return physicalCoreCount;
        }

        return Environment.ProcessorCount;
    }

    #region Windows Specific
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public IntPtr ProcessorMask;
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public ProcessorInformation ProcessorInformation;
    }

    // ReSharper disable once InconsistentNaming
    // ReSharper disable once EnumUnderlyingTypeIsInt
    internal enum LOGICAL_PROCESSOR_RELATIONSHIP : int
    {
        RelationProcessorCore,
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct ProcessorInformation
    {
        [FieldOffset(0)]
        public byte Flags;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformation(nint buffer, ref uint returnLength);
    #endregion
}
