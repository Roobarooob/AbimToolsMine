using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AbimToolsMine
{
    internal sealed class IfcExportConfigurationLoader
    {
        private const string ConfigurationTypeName = "BIM.IFC.Export.UI.IFCExportConfiguration";
        private readonly object configuration;
        private readonly MethodInfo updateOptionsMethod;

        private IfcExportConfigurationLoader(object configuration, MethodInfo updateOptionsMethod, bool requiresView)
        {
            this.configuration = configuration;
            this.updateOptionsMethod = updateOptionsMethod;
            RequiresView = requiresView;
        }

        public bool RequiresView { get; }

        public static IfcExportConfigurationLoader Load(Autodesk.Revit.ApplicationServices.Application application, string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                return null;
            }

            string fullPath = Path.GetFullPath(configPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Файл JSON-конфигурации IFC не найден.", fullPath);
            }

            string json = File.ReadAllText(fullPath);
            JObject jsonObject;
            try
            {
                jsonObject = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Некорректный JSON в конфигурации IFC '{fullPath}'.", ex);
            }

            Type configurationType = FindConfigurationType(application);
            object configuration;
            try
            {
                configuration = JsonConvert.DeserializeObject(json, configurationType);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Конфигурация IFC '{fullPath}' не соответствует формату экспортера Revit.", ex);
            }

            if (configuration == null)
            {
                throw new InvalidDataException($"Не удалось прочитать конфигурацию IFC '{fullPath}'.");
            }

            MethodInfo updateOptionsMethod = configurationType.GetMethod(
                "UpdateOptions",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IFCExportOptions), typeof(ElementId) },
                null);

            if (updateOptionsMethod == null)
            {
                throw new MissingMethodException(configurationType.FullName, "UpdateOptions(IFCExportOptions, ElementId)");
            }

            bool requiresView = GetBoolean(jsonObject, "VisibleElementsOfCurrentView") ||
                                GetBoolean(jsonObject, "UseActiveViewGeometry") ||
                                GetBoolean(jsonObject, "ExportRoomsInView");

            return new IfcExportConfigurationLoader(configuration, updateOptionsMethod, requiresView);
        }

        public void UpdateOptions(IFCExportOptions options, ElementId viewId)
        {
            try
            {
                updateOptionsMethod.Invoke(configuration, new object[] { options, viewId });
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    "Экспортер Revit не смог применить выбранную JSON-конфигурацию IFC.",
                    ex.InnerException ?? ex);
            }
        }

        private static bool GetBoolean(JObject jsonObject, string propertyName)
        {
            JToken value = jsonObject.GetValue(propertyName, StringComparison.OrdinalIgnoreCase);
            return value != null && value.Type == JTokenType.Boolean && value.Value<bool>();
        }

        private static Type FindConfigurationType(Autodesk.Revit.ApplicationServices.Application application)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(ConfigurationTypeName, false))
                .FirstOrDefault(candidate => candidate != null);

            if (type != null)
            {
                return type;
            }

            foreach (string candidatePath in GetAssemblyCandidatePaths(application))
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                try
                {
                    Assembly assembly = Assembly.LoadFrom(candidatePath);
                    type = assembly.GetType(ConfigurationTypeName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception ex) when (ex is FileLoadException || ex is BadImageFormatException)
                {
                    // Проверяем следующий штатный путь установки экспортера IFC.
                }
            }

            throw new InvalidOperationException(
                $"Не найден штатный модуль экспорта IFC для Revit {application.VersionNumber}. " +
                "Проверьте установку компонента Autodesk IFC Exporter.");
        }

        private static IEnumerable<string> GetAssemblyCandidatePaths(Autodesk.Revit.ApplicationServices.Application application)
        {
            string revitFolder = Path.GetDirectoryName(typeof(IFCExportOptions).Assembly.Location);

            yield return Path.Combine(revitFolder, "AddIns", "IFCExporterUI", "Autodesk.IFC.Export.UI.dll");
            yield return Path.Combine(revitFolder, "AddIns", "Autodesk.IFC.Export.UI", "Autodesk.IFC.Export.UI.dll");
        }
    }
}
