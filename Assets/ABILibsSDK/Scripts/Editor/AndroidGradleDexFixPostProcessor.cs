#if UNITY_ANDROID
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace ABILibsSDK.Editor
{
    /// <summary>
    /// Patches generated launcher/build.gradle with D8-friendly dependency pins (JDK 11 / Unity 2022.3).
    /// </summary>
    internal sealed class AndroidGradleDexFixPostProcessor : IPostGenerateGradleAndroidProject
    {
        private const string LogPrefix = "[ABI-SDK]";
        private const string DexFixBlock = @"
configurations.configureEach {
    resolutionStrategy {
        force 'com.google.errorprone:error_prone_annotations:2.20.0'
        force 'androidx.webkit:webkit:1.11.0'
    }
}
";

        public int callbackOrder => 10002;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!TryResolveLauncherGradlePath(path, out var launcherGradlePath))
            {
                Debug.LogWarning($"{LogPrefix} Could not find launcher/build.gradle under `{path}`.");
                return;
            }

            var gradle = File.ReadAllText(launcherGradlePath);
            var patched = PatchLauncherGradle(gradle);
            if (patched == gradle)
            {
                return;
            }

            File.WriteAllText(launcherGradlePath, patched);
            Debug.Log($"{LogPrefix} Patched launcher/build.gradle (D8 dependency pins, Java 11).");
        }

        private static string PatchLauncherGradle(string gradle)
        {
            var patched = gradle.Replace("JavaVersion.VERSION_17", "JavaVersion.VERSION_11");

            if (!patched.Contains("error_prone_annotations:2.20.0", StringComparison.Ordinal))
            {
                patched = Regex.Replace(
                    patched,
                    @"(apply plugin:[^\n]+\n(?:apply plugin:[^\n]+\n)*)",
                    "$1" + DexFixBlock,
                    RegexOptions.Multiline);
            }

            return patched;
        }

        private static bool TryResolveLauncherGradlePath(string generatedModulePath, out string launcherGradlePath)
        {
            launcherGradlePath = null;
            if (string.IsNullOrWhiteSpace(generatedModulePath))
            {
                return false;
            }

            var modulePath = Path.GetFullPath(generatedModulePath.Trim());
            var candidates = new[]
            {
                Path.Combine(modulePath, "..", "launcher", "build.gradle"),
                Path.Combine(modulePath, "launcher", "build.gradle"),
                Path.Combine(Directory.GetParent(modulePath)?.FullName ?? modulePath, "launcher", "build.gradle"),
            };

            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                if (File.Exists(full))
                {
                    launcherGradlePath = full;
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
