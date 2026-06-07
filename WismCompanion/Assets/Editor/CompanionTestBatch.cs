using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace WismCompanion.Editor
{
    public static class CompanionTestBatch
    {
        private const string DefaultResultsPath = "Logs/companion-editmode-results.xml";

        public static void RunEditModeTests()
        {
            var resultsPath = GetArg("-testResults", DefaultResultsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath) ?? "Logs");

            var callbacks = new ResultCallbacks(resultsPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(callbacks);

            var settings = new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "WismCompanion.Tests" }
            })
            {
                runSynchronously = true
            };

            Debug.Log("Running WismCompanion EditMode tests.");
            api.Execute(settings);
            api.UnregisterCallbacks(callbacks);

            if (callbacks.Result == null)
            {
                Debug.LogError("WismCompanion EditMode tests did not produce a result.");
                EditorApplication.Exit(3);
                return;
            }

            var failed = callbacks.Result.FailCount;
            Debug.Log($"WismCompanion EditMode tests complete. Passed={callbacks.Result.PassCount} Failed={failed} Skipped={callbacks.Result.SkipCount}");
            EditorApplication.Exit(failed == 0 ? 0 : 2);
        }

        private static string GetArg(string name, string fallback)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            private readonly string resultsPath;

            public ResultCallbacks(string resultsPath)
            {
                this.resultsPath = resultsPath;
            }

            public ITestResultAdaptor Result { get; private set; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Result = result;
                TestRunnerApi.SaveResultToFile(result, resultsPath);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
