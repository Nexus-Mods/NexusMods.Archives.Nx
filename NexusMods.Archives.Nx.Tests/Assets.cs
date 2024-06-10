namespace NexusMods.Archives.Nx.Tests;

/// <summary>
///     Provides access to test assets.
/// </summary>
public static class Assets
{
    private static string TestRunnerDirectory => AppContext.BaseDirectory;

    /// <summary>
    ///     This folder stores all tests for repacking .nx archives.
    /// </summary>
    public static class Repacks
    {
        public static class Unit
        {
            /// <summary>
            ///     We've changed a file in a SOLID block. The block should be repacked.
            /// </summary>
            public static class ChangeInSolidBlock
            {
                public static string New => Path.Combine(TestRunnerDirectory, "Assets/Repacks/Unit/Change-In-Solid-Block/New");
                public static string Original => Path.Combine(TestRunnerDirectory, "Assets/Repacks/Unit/Change-In-Solid-Block/Original");
            }

            /// <summary>
            ///     A chunked file is left unchanged. It should be reused.
            ///     Run this test with chunk size of 32K.
            /// </summary>
            public static class ChunkedFileUnchanged
            {
                public static string Original => Path.Combine(TestRunnerDirectory, "Assets/Repacks/Unit/Chunked-File-Unchanged/Original");
            }
        }

        public static class Integration
        {
            /// <summary>
            ///     We've changed a file in a SOLID block. The block should be repacked.
            ///     But there's also another block that's unchanged.
            /// </summary>
            public static class ChangeInSolidBlockWithAnotherUnchangedBlock
            {
                public static string New => Path.Combine(TestRunnerDirectory, "Assets/Repacks/Integration/Change-In-Solid-Block-With-Another-Unchanged-Block/New");
                public static string Original => Path.Combine(TestRunnerDirectory, "Assets/Repacks/Integration/Change-In-Solid-Block-With-Another-Unchanged-Block/Original");
            }
        }
    }
}
