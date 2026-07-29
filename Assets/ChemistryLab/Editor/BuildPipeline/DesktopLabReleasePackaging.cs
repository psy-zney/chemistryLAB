using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ChemistryLab.Desktop.Editor
{
    internal static class DesktopLabReleasePackaging
    {
        internal const string BuildRoot = "Builds/ChemistryLab3D";
        internal const string DistributionDirectory = BuildRoot + "/Windows-x64";
        internal const string ExecutablePath = DistributionDirectory + "/ChemistryLab3D.exe";
        internal const string DataDirectoryName = "ChemistryLab3D_Data";
        internal const string PackageDirectory = BuildRoot + "/Packages";

        private const string ArchiveRoot = "ChemistryLab3D-Windows-x64";
        private const string ManifestFileName = "build-manifest.json";
        private const string ReadmeFileName = "README.txt";

        internal static string PrepareCleanOutput(string projectRoot)
        {
            var absoluteBuildRoot = ResolveInsideProject(projectRoot, BuildRoot);
            var expectedParent = ResolveInsideProject(projectRoot, "Builds");
            if (!IsDirectChildOf(absoluteBuildRoot, expectedParent))
            {
                throw new InvalidOperationException(
                    "Refusing to clean an unexpected build directory: " + absoluteBuildRoot);
            }

            if (Directory.Exists(absoluteBuildRoot))
            {
                Directory.Delete(absoluteBuildRoot, true);
            }

            var distribution = ResolveInsideProject(projectRoot, DistributionDirectory);
            Directory.CreateDirectory(distribution);
            return distribution;
        }

        internal static ReleasePackage CreatePortablePackage(string projectRoot)
        {
            var distribution = ResolveInsideProject(projectRoot, DistributionDirectory);
            RemoveDoNotShipDirectories(distribution);
            ValidateRuntimeLayoutOrThrow(distribution);

            var packageDirectory = ResolveInsideProject(projectRoot, PackageDirectory);
            Directory.CreateDirectory(packageDirectory);
            var version = SanitizeVersion(Application.version);
            var archiveFileName = "ChemistryLab3D-Windows-x64-v" + version + ".zip";
            var relativeArchivePath = PackageDirectory + "/" + archiveFileName;
            var absoluteArchivePath = Path.Combine(packageDirectory, archiveFileName);
            var absoluteChecksumPath = absoluteArchivePath + ".sha256";

            WriteDistributionReadme(distribution);
            var runtimeFiles = EnumerateDistributionFiles(distribution);
            var runtimeBytes = SumFileBytes(runtimeFiles);
            WriteManifest(
                distribution,
                relativeArchivePath,
                runtimeFiles.Count + 1,
                runtimeBytes);

            runtimeFiles = EnumerateDistributionFiles(distribution);
            runtimeBytes = SumFileBytes(runtimeFiles);
            CreateArchive(distribution, absoluteArchivePath, runtimeFiles);
            var checksum = ComputeSha256(absoluteArchivePath);
            File.WriteAllText(
                absoluteChecksumPath,
                checksum + "  " + archiveFileName + Environment.NewLine,
                new UTF8Encoding(false));

            ValidateArchiveOrThrow(absoluteArchivePath, runtimeFiles.Count);
            return new ReleasePackage
            {
                distributionPath = DistributionDirectory,
                executablePath = ExecutablePath,
                dataDirectory = DistributionDirectory + "/" + DataDirectoryName,
                archivePath = relativeArchivePath,
                checksumPath = relativeArchivePath + ".sha256",
                archiveSha256 = checksum,
                runtimeFileCount = runtimeFiles.Count,
                runtimeSizeBytes = runtimeBytes,
                archiveSizeBytes = new FileInfo(absoluteArchivePath).Length
            };
        }

        internal static void ValidateExistingPackageOrThrow(string projectRoot)
        {
            var distribution = ResolveInsideProject(projectRoot, DistributionDirectory);
            ValidateRuntimeLayoutOrThrow(distribution);

            var manifestPath = Path.Combine(distribution, ManifestFileName);
            var readmePath = Path.Combine(distribution, ReadmeFileName);
            if (!File.Exists(manifestPath) || !File.Exists(readmePath))
            {
                throw new InvalidOperationException(
                    "Portable build is missing its manifest or README.");
            }

            var archiveName = "ChemistryLab3D-Windows-x64-v"
                + SanitizeVersion(Application.version) + ".zip";
            var archivePath = Path.Combine(
                ResolveInsideProject(projectRoot, PackageDirectory),
                archiveName);
            var checksumPath = archivePath + ".sha256";
            if (!File.Exists(archivePath) || !File.Exists(checksumPath))
            {
                throw new InvalidOperationException(
                    "Portable build is missing its ZIP or SHA-256 checksum.");
            }

            var files = EnumerateDistributionFiles(distribution);
            ValidateArchiveOrThrow(archivePath, files.Count);
            var expectedChecksum = File.ReadAllText(checksumPath)
                .Trim()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
            var actualChecksum = ComputeSha256(archivePath);
            if (!string.Equals(
                    expectedChecksum,
                    actualChecksum,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Portable archive SHA-256 checksum does not match.");
            }
        }

        private static void ValidateRuntimeLayoutOrThrow(string distribution)
        {
            var requiredPaths = new[]
            {
                Path.Combine(distribution, "ChemistryLab3D.exe"),
                Path.Combine(distribution, DataDirectoryName),
                Path.Combine(distribution, "UnityPlayer.dll"),
                Path.Combine(distribution, "UnityCrashHandler64.exe"),
                Path.Combine(distribution, "MonoBleedingEdge")
            };

            for (var index = 0; index < requiredPaths.Length; index++)
            {
                var path = requiredPaths[index];
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    throw new InvalidOperationException(
                        "Windows distribution is missing a required runtime path: " + path);
                }
            }

            var dataPath = Path.Combine(distribution, DataDirectoryName);
            if (!File.Exists(Path.Combine(dataPath, "globalgamemanagers"))
                || !Directory.Exists(Path.Combine(dataPath, "Managed")))
            {
                throw new InvalidOperationException(
                    "Unity data directory is incomplete: " + dataPath);
            }

            var doNotShip = Directory.GetDirectories(
                distribution,
                "*_BurstDebugInformation_DoNotShip",
                SearchOption.TopDirectoryOnly);
            if (doNotShip.Length > 0)
            {
                throw new InvalidOperationException(
                    "Distribution still contains Burst debug data marked DoNotShip.");
            }
        }

        private static void RemoveDoNotShipDirectories(string distribution)
        {
            var directories = Directory.GetDirectories(
                distribution,
                "*_BurstDebugInformation_DoNotShip",
                SearchOption.TopDirectoryOnly);
            for (var index = 0; index < directories.Length; index++)
            {
                Directory.Delete(directories[index], true);
            }
        }

        private static void WriteDistributionReadme(string distribution)
        {
            var content =
                "CHEMISTRY LAB 3D · WINDOWS x64" + Environment.NewLine
                + "================================" + Environment.NewLine
                + Environment.NewLine
                + "Chạy game: mở ChemistryLab3D.exe." + Environment.NewLine
                + "Giữ nguyên toàn bộ nội dung trong thư mục này. Unity yêu cầu "
                + "ChemistryLab3D_Data và các DLL nằm cạnh file EXE." + Environment.NewLine
                + Environment.NewLine
                + "Run the game by opening ChemistryLab3D.exe." + Environment.NewLine
                + "Keep this folder intact. The _Data directory, UnityPlayer.dll, "
                + "and MonoBleedingEdge are required runtime files." + Environment.NewLine
                + Environment.NewLine
                + "Controls: WASD move · Mouse look · E interact · ESC menu"
                + Environment.NewLine;
            File.WriteAllText(
                Path.Combine(distribution, ReadmeFileName),
                content,
                new UTF8Encoding(false));
        }

        private static void WriteManifest(
            string distribution,
            string archivePath,
            int packagedFileCount,
            long payloadSizeBytes)
        {
            var manifest = new DistributionManifest
            {
                schemaVersion = "1.0",
                product = "Chemistry Lab 3D",
                version = Application.version,
                unityVersion = Application.unityVersion,
                platform = "Windows x64",
                executable = "ChemistryLab3D.exe",
                dataDirectory = DataDirectoryName,
                archive = archivePath,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                packagedFileCount = packagedFileCount,
                payloadSizeBytes = payloadSizeBytes,
                keepDirectoryIntact = true
            };
            File.WriteAllText(
                Path.Combine(distribution, ManifestFileName),
                JsonUtility.ToJson(manifest, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void CreateArchive(
            string distribution,
            string absoluteArchivePath,
            IReadOnlyList<string> files)
        {
            using (var stream = new FileStream(
                       absoluteArchivePath,
                       FileMode.Create,
                       FileAccess.ReadWrite,
                       FileShare.None))
            using (var archive = new ZipArchive(
                       stream,
                       ZipArchiveMode.Create,
                       false,
                       Encoding.UTF8))
            {
                for (var index = 0; index < files.Count; index++)
                {
                    var file = files[index];
                    var relative = GetRelativePath(distribution, file);
                    var entryName = ArchiveRoot + "/" + relative.Replace('\\', '/');
                    var entry = archive.CreateEntry(
                        entryName,
                        System.IO.Compression.CompressionLevel.Optimal);
                    using (var input = File.OpenRead(file))
                    using (var output = entry.Open())
                    {
                        input.CopyTo(output);
                    }
                }
            }
        }

        private static void ValidateArchiveOrThrow(string archivePath, int expectedFileCount)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                if (archive.Entries.Count != expectedFileCount)
                {
                    throw new InvalidOperationException(
                        "Portable archive entry count mismatch: expected "
                        + expectedFileCount + ", got " + archive.Entries.Count + ".");
                }

                var executableEntry = ArchiveRoot + "/ChemistryLab3D.exe";
                var dataEntryPrefix = ArchiveRoot + "/" + DataDirectoryName + "/";
                var hasExecutable = false;
                var hasData = false;
                for (var index = 0; index < archive.Entries.Count; index++)
                {
                    var name = archive.Entries[index].FullName;
                    hasExecutable |= string.Equals(name, executableEntry, StringComparison.Ordinal);
                    hasData |= name.StartsWith(dataEntryPrefix, StringComparison.Ordinal);
                    if (name.IndexOf(
                            "BurstDebugInformation_DoNotShip",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        throw new InvalidOperationException(
                            "Portable archive contains Burst debug data marked DoNotShip.");
                    }
                }

                if (!hasExecutable || !hasData)
                {
                    throw new InvalidOperationException(
                        "Portable archive does not contain the executable and Unity data root.");
                }
            }
        }

        private static List<string> EnumerateDistributionFiles(string distribution)
        {
            var files = new List<string>(
                Directory.GetFiles(distribution, "*", SearchOption.AllDirectories));
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        private static long SumFileBytes(IReadOnlyList<string> files)
        {
            long total = 0L;
            for (var index = 0; index < files.Count; index++)
            {
                total += new FileInfo(files[index]).Length;
            }

            return total;
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var hash = algorithm.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string GetRelativePath(string root, string path)
        {
            var rootUri = new Uri(AppendDirectorySeparator(root));
            var pathUri = new Uri(path);
            return Uri.UnescapeDataString(
                rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string ResolveInsideProject(string projectRoot, string relativePath)
        {
            var absoluteProjectRoot = AppendDirectorySeparator(Path.GetFullPath(projectRoot));
            var absolutePath = Path.GetFullPath(
                Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!absolutePath.StartsWith(absoluteProjectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Build path escaped the Unity project: " + absolutePath);
            }

            return absolutePath;
        }

        private static bool IsDirectChildOf(string path, string expectedParent)
        {
            var parent = Directory.GetParent(Path.GetFullPath(path));
            return parent != null
                && string.Equals(
                    parent.FullName.TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(expectedParent).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
        }

        private static string SanitizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return "1.0";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(version.Length);
            for (var index = 0; index < version.Length; index++)
            {
                builder.Append(Array.IndexOf(invalid, version[index]) >= 0 ? '-' : version[index]);
            }

            return builder.ToString();
        }

        [Serializable]
        private sealed class DistributionManifest
        {
            public string schemaVersion;
            public string product;
            public string version;
            public string unityVersion;
            public string platform;
            public string executable;
            public string dataDirectory;
            public string archive;
            public string generatedAtUtc;
            public int packagedFileCount;
            public long payloadSizeBytes;
            public bool keepDirectoryIntact;
        }
    }

    [Serializable]
    internal sealed class ReleasePackage
    {
        public string distributionPath;
        public string executablePath;
        public string dataDirectory;
        public string archivePath;
        public string checksumPath;
        public string archiveSha256;
        public int runtimeFileCount;
        public long runtimeSizeBytes;
        public long archiveSizeBytes;
    }
}
