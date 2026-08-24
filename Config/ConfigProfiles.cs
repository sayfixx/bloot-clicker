using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Autoclicker.Config
{
    internal static class ConfigProfiles
    {
        private static string Root
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "elegies", "configs"); }
        }

        private static string ActivePath
        {
            get { return Path.Combine(Root, "active.profile"); }
        }

        public static string[] GetNames()
        {
            try
            {
                if (!Directory.Exists(Root)) return new string[0];
                EnforceMaximumProfiles(GetActiveName());
                return Directory.GetFiles(Root, "*.xml", SearchOption.TopDirectoryOnly)
                    .Select(path => new { Path = path, Name = Path.GetFileNameWithoutExtension(path) })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .OrderByDescending(x => File.GetLastWriteTimeUtc(x.Path))
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .Select(x => x.Name)
                    .ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        public static string GetActiveName()
        {
            try
            {
                if (!File.Exists(ActivePath)) return "";
                return SafeName(File.ReadAllText(ActivePath));
            }
            catch
            {
                return "";
            }
        }

        public static void SetActiveName(string name)
        {
            try
            {
                string safe = SafeName(name);
                if (string.IsNullOrWhiteSpace(safe)) return;
                Directory.CreateDirectory(Root);
                File.WriteAllText(ActivePath, safe);
            }
            catch
            {
            }
        }

        private static string SafeName(string name)
        {
            name = (name ?? "").Trim();

            while (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
                else
                    name = name.Substring(0, name.Length - 5);
                name = name.Trim();
            }

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return Regex.Replace(name, @"\s+", " ").Trim();
        }

        private static string GetPath(string name)
        {
            return Path.Combine(Root, SafeName(name) + ".xml");
        }

        private sealed class JsonConfigPackage
        {
            public string Name { get; set; }
            public string ConfigXml { get; set; }
            public string CharacterImageBase64 { get; set; }
            public string CharacterImageExtension { get; set; }
            public string BackgroundImageBase64 { get; set; }
            public string BackgroundImageExtension { get; set; }
        }

        public static bool SaveJson(MainWindow mw, string path, string name)
        {
            try
            {
                ConfigIO.SaveSilent(mw);
                string current = Path.Combine(mw.ConfigDirectory, mw.ConfigFileName);
                if (!File.Exists(current)) return false;

                string xml = File.ReadAllText(current);
                var package = new JsonConfigPackage
                {
                    Name = SafeName(name),
                    ConfigXml = xml,
                    CharacterImageBase64 = ReadImageBase64(mw.CharacterImagePath, out string characterExtension),
                    CharacterImageExtension = characterExtension,
                    BackgroundImageBase64 = ReadImageBase64(mw.BackgroundImagePath, out string backgroundExtension),
                    BackgroundImageExtension = backgroundExtension
                };

                File.WriteAllText(path, JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true }));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ExportJson(string name, string path)
        {
            try
            {
                string source = GetPath(name);
                if (!File.Exists(source)) return false;

                string xml = File.ReadAllText(source);
                var document = XDocument.Parse(xml);
                XElement settings = document.Root?.Element("settings");
                string characterPath = settings?.Element("CharacterImagePath")?.Value ?? "";
                string backgroundPath = settings?.Element("BackgroundImagePath")?.Value ?? "";

                var package = new JsonConfigPackage
                {
                    Name = SafeName(name),
                    ConfigXml = xml,
                    CharacterImageBase64 = ReadImageBase64(characterPath, out string characterExtension),
                    CharacterImageExtension = characterExtension,
                    BackgroundImageBase64 = ReadImageBase64(backgroundPath, out string backgroundExtension),
                    BackgroundImageExtension = backgroundExtension
                };

                File.WriteAllText(path, JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true }));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ImportJson(string path, out string importedName)
        {
            importedName = "";
            try
            {
                string json = File.ReadAllText(path);
                var package = JsonSerializer.Deserialize<JsonConfigPackage>(json);

                if (package == null || string.IsNullOrWhiteSpace(package.ConfigXml))
                    return false;

                string name = SafeName(package.Name);
                if (string.IsNullOrWhiteSpace(name))
                    name = SafeName(Path.GetFileNameWithoutExtension(path));
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                Directory.CreateDirectory(Root);

                string destination = GetPath(name);
                if (File.Exists(destination))
                {
                    string baseName = name;
                    int index = 2;
                    while (File.Exists(destination))
                    {
                        name = baseName + " " + index++;
                        destination = GetPath(name);
                    }
                }

                string xml = package.ConfigXml;
                var document = XDocument.Parse(xml);
                XElement settings = document.Root?.Element("settings");
                if (settings == null) return false;

                string characterAsset = RestoreImage(package.CharacterImageBase64, package.CharacterImageExtension, name, "character");
                string backgroundAsset = RestoreImage(package.BackgroundImageBase64, package.BackgroundImageExtension, name, "background");

                if (!string.IsNullOrWhiteSpace(characterAsset))
                    SetElement(settings, "CharacterImagePath", characterAsset);

                if (!string.IsNullOrWhiteSpace(backgroundAsset))
                    SetElement(settings, "BackgroundImagePath", backgroundAsset);

                document.Save(destination);
                try { File.SetAttributes(destination, FileAttributes.Normal); } catch { }
                EnforceMaximumProfiles(name);

                importedName = name;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Save(MainWindow mw, string name)
        {
            string safe = SafeName(name);
            if (string.IsNullOrWhiteSpace(safe)) return false;

            try
            {
                ConfigIO.SaveSilent(mw);
                string current = Path.Combine(mw.ConfigDirectory, mw.ConfigFileName);
                if (!File.Exists(current)) return false;

                Directory.CreateDirectory(Root);
                string destination = GetPath(safe);
                File.Copy(current, destination, true);
                try { File.SetAttributes(destination, FileAttributes.Normal); } catch { }
                EnforceMaximumProfiles(safe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Load(MainWindow mw, string name)
        {
            try
            {
                string safe = SafeName(name);
                string source = GetPath(safe);
                if (!File.Exists(source)) return false;

                string tempDir = ConfigPaths.GetDeepRandom();
                Directory.CreateDirectory(tempDir);
                string tempFile = Path.Combine(tempDir, ConfigPaths.GenerateFileName());
                File.Copy(source, tempFile, true);

                try { File.SetAttributes(tempFile, FileAttributes.Hidden | FileAttributes.System); } catch { }

                mw.ConfigDirectory = tempDir;
                mw.ConfigFileName = Path.GetFileName(tempFile);
                ConfigPaths.SaveInfo(mw);
                ConfigIO.Load(mw);
                SetActiveName(safe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Delete(string name)
        {
            try
            {
                string path = GetPath(name);
                if (!File.Exists(path)) return false;

                File.Delete(path);

                if (string.Equals(GetActiveName(), SafeName(name), StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(ActivePath); } catch { }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Rename(string oldName, string newName)
        {
            string oldSafe = SafeName(oldName);
            string newSafe = SafeName(newName);
            if (string.IsNullOrWhiteSpace(oldSafe) || string.IsNullOrWhiteSpace(newSafe)) return false;

            try
            {
                string oldPath = GetPath(oldSafe);
                string newPath = GetPath(newSafe);

                if (!File.Exists(oldPath) || File.Exists(newPath))
                    return false;

                File.Move(oldPath, newPath);

                if (string.Equals(GetActiveName(), oldSafe, StringComparison.OrdinalIgnoreCase))
                    SetActiveName(newSafe);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void EnforceMaximumProfiles(string keepName)
        {
            try
            {
                if (!Directory.Exists(Root)) return;

                var files = Directory.GetFiles(Root, "*.xml", SearchOption.TopDirectoryOnly)
                    .Select(path => new
                    {
                        Path = path,
                        Name = Path.GetFileNameWithoutExtension(path),
                        LastWrite = File.GetLastWriteTimeUtc(path)
                    })
                    .OrderByDescending(x => x.LastWrite)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                while (files.Count > 3)
                {
                    var candidate = files.LastOrDefault(x => !string.Equals(x.Name, SafeName(keepName), StringComparison.OrdinalIgnoreCase));
                    if (candidate == null) break;
                    try { File.Delete(candidate.Path); } catch { break; }
                    files.Remove(candidate);
                }
            }
            catch
            {
            }
        }

        private static string ReadImageBase64(string path, out string extension)
        {
            extension = "";
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return "";

                extension = NormalizeExtension(Path.GetExtension(path));
                if (string.IsNullOrWhiteSpace(extension))
                    return "";

                return Convert.ToBase64String(File.ReadAllBytes(path));
            }
            catch
            {
                extension = "";
                return "";
            }
        }

        private static string RestoreImage(string base64, string extension, string configName, string kind)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64))
                    return "";

                extension = NormalizeExtension(extension);
                if (string.IsNullOrWhiteSpace(extension))
                    return "";

                byte[] data = Convert.FromBase64String(base64);
                string assetDirectory = Path.Combine(Root, "assets");
                Directory.CreateDirectory(assetDirectory);

                string fileName = SafeName(configName) + "_" + kind + "_" + Guid.NewGuid().ToString("N") + extension;
                string path = Path.Combine(assetDirectory, fileName);
                File.WriteAllBytes(path, data);
                return path;
            }
            catch
            {
                return "";
            }
        }

        private static string NormalizeExtension(string extension)
        {
            extension = (extension ?? "").Trim().ToLowerInvariant();
            if (extension == ".jpeg") return ".jpg";

            if (extension == ".gif" || extension == ".png" || extension == ".jpg" || extension == ".bmp")
                return extension;

            return "";
        }

        private static void SetElement(XElement parent, string name, string value)
        {
            XElement element = parent.Element(name);
            if (element == null)
                parent.Add(new XElement(name, value));
            else
                element.Value = value ?? "";
        }
    }
}
