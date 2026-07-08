using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class ShaftMarks : IExternalCommand
    {
        private static readonly Guid SettingsGuid = new Guid("8C90A2E7-1D8A-43AB-8ED8-8701971346A1");
        private static readonly Guid MarkGuid = new Guid("D4E06093-E472-430D-8279-359F97BCDA11");

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<ViewPlan> plans = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan))
                .ToList();
            List<TextNoteType> textTypes = new FilteredElementCollector(doc).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
            if (!textTypes.Any())
            {
                TaskDialog.Show("Марки по шахтам", "В проекте нет типов текста.");
                return Result.Cancelled;
            }

            Settings settings = LoadSettings(doc);
            var window = new ShaftMarksWindow(plans, textTypes, settings.ParameterName, settings.UseCurrentView,
                settings.ViewUniqueIds, settings.TextTypeId);
            RevitWindowOwner.SetOwner(window, commandData.Application);
            if (window.ShowDialog() != true) return Result.Cancelled;

            List<ViewPlan> targetViews;
            if (window.UseCurrentView)
            {
                ViewPlan activePlan = doc.ActiveView as ViewPlan;
                if (activePlan == null || (activePlan.ViewType != ViewType.FloorPlan && activePlan.ViewType != ViewType.CeilingPlan))
                {
                    TaskDialog.Show("Марки по шахтам", "Активный вид должен быть планом этажа или планом потолка.");
                    return Result.Cancelled;
                }
                targetViews = new List<ViewPlan> { activePlan };
            }
            else
            {
                var ids = new HashSet<string>(window.SelectedViewUniqueIds);
                targetViews = plans.Where(v => ids.Contains(v.UniqueId)).ToList();
            }

            int created = 0, withoutValue = 0, withoutGeometry = 0, replaced = 0;
            using (Transaction transaction = new Transaction(doc, "Марки по шахтам"))
            {
                transaction.Start();
                Schema markSchema = GetOrCreateMarkSchema();
                foreach (ViewPlan view in targetViews)
                {
                    var oldMarks = new FilteredElementCollector(doc, view.Id).OfClass(typeof(TextNote))
                        .Where(e => e.GetEntity(markSchema).IsValid()).Select(e => e.Id).ToList();
                    if (oldMarks.Any()) { doc.Delete(oldMarks); replaced += oldMarks.Count; }

                    List<Element> shafts = new FilteredElementCollector(doc, view.Id)
                        .OfCategory(BuiltInCategory.OST_ShaftOpening).WhereElementIsNotElementType().ToElements().ToList();
                    foreach (Element shaft in shafts)
                    {
                        string text = ParameterText(FindParameter(doc, shaft, window.ParameterName));
                        if (string.IsNullOrWhiteSpace(text)) { withoutValue++; continue; }
                        BoundingBoxXYZ box = shaft.get_BoundingBox(view);
                        if (box == null) { withoutGeometry++; continue; }
                        XYZ point = CenterOnViewPlane(view, box);
                        var options = new TextNoteOptions(window.TextTypeId)
                        {
                            HorizontalAlignment = HorizontalTextAlignment.Center
                        };
                        TextNote note = TextNote.Create(doc, view.Id, point, text, options);
                        Entity markEntity = new Entity(markSchema);
                        markEntity.Set(markSchema.GetField("SourceElementUniqueId"), shaft.UniqueId);
                        note.SetEntity(markEntity);
                        created++;
                    }
                }

                SaveSettings(doc, new Settings
                {
                    ParameterName = window.ParameterName,
                    UseCurrentView = window.UseCurrentView,
                    ViewUniqueIds = window.SelectedViewUniqueIds.ToList(),
                    TextTypeId = window.TextTypeId
                });
                transaction.Commit();
            }

            TaskDialog.Show("Марки по шахтам",
                $"Обработано видов: {targetViews.Count}\nСоздано марок: {created}\nЗаменено прежних марок: {replaced}\nБез значения параметра: {withoutValue}\nБез видимой геометрии: {withoutGeometry}");
            return Result.Succeeded;
        }

        private static Parameter FindParameter(Document doc, Element element, string name)
        {
            Parameter parameter = element.LookupParameter(name);
            if (parameter != null) return parameter;
            ElementId typeId = element.GetTypeId();
            return typeId == ElementId.InvalidElementId ? null : doc.GetElement(typeId)?.LookupParameter(name);
        }

        private static string ParameterText(Parameter parameter)
        {
            if (parameter == null || !parameter.HasValue) return null;
            string formatted = parameter.AsValueString();
            if (!string.IsNullOrWhiteSpace(formatted)) return formatted;
            switch (parameter.StorageType)
            {
                case StorageType.String: return parameter.AsString();
                case StorageType.Integer: return parameter.AsInteger().ToString();
                case StorageType.Double: return parameter.AsDouble().ToString();
                case StorageType.ElementId: return parameter.AsElementId().ToString();
                default: return null;
            }
        }

        private static XYZ CenterOnViewPlane(View view, BoundingBoxXYZ box)
        {
            XYZ center = (box.Min + box.Max) * 0.5;
            XYZ right = view.RightDirection.Normalize();
            XYZ up = view.UpDirection.Normalize();
            XYZ delta = center - view.Origin;
            return view.Origin + right.Multiply(delta.DotProduct(right)) + up.Multiply(delta.DotProduct(up));
        }

        private static Settings LoadSettings(Document doc)
        {
            var result = new Settings { UseCurrentView = true, ViewUniqueIds = new List<string>(), TextTypeId = ElementId.InvalidElementId };
            Schema schema = Schema.Lookup(SettingsGuid);
            if (schema == null) return result;
            Entity entity = doc.ProjectInformation.GetEntity(schema);
            if (!entity.IsValid()) return result;
            result.ParameterName = entity.Get<string>(schema.GetField("ParameterName")) ?? string.Empty;
            result.UseCurrentView = entity.Get<int>(schema.GetField("UseCurrentView")) != 0;
            result.ViewUniqueIds = (entity.Get<string>(schema.GetField("ViewUniqueIds")) ?? string.Empty)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            result.TextTypeId = entity.Get<ElementId>(schema.GetField("TextTypeId"));
            return result;
        }

        private static void SaveSettings(Document doc, Settings settings)
        {
            Schema schema = GetOrCreateSettingsSchema();
            Entity entity = new Entity(schema);
            entity.Set(schema.GetField("ParameterName"), settings.ParameterName ?? string.Empty);
            entity.Set(schema.GetField("UseCurrentView"), settings.UseCurrentView ? 1 : 0);
            entity.Set(schema.GetField("ViewUniqueIds"), string.Join("\n", settings.ViewUniqueIds));
            entity.Set(schema.GetField("TextTypeId"), settings.TextTypeId);
            doc.ProjectInformation.SetEntity(entity);
        }

        private static Schema GetOrCreateSettingsSchema()
        {
            Schema schema = Schema.Lookup(SettingsGuid);
            if (schema != null) return schema;
            var builder = new SchemaBuilder(SettingsGuid);
            builder.SetSchemaName("AbimToolsMineShaftMarksSettings"); builder.SetVendorId("ABIM");
            builder.SetReadAccessLevel(AccessLevel.Public); builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField("ParameterName", typeof(string));
            builder.AddSimpleField("UseCurrentView", typeof(int));
            builder.AddSimpleField("ViewUniqueIds", typeof(string));
            builder.AddSimpleField("TextTypeId", typeof(ElementId));
            return builder.Finish();
        }

        private static Schema GetOrCreateMarkSchema()
        {
            Schema schema = Schema.Lookup(MarkGuid);
            if (schema != null) return schema;
            var builder = new SchemaBuilder(MarkGuid);
            builder.SetSchemaName("AbimToolsMineShaftMark"); builder.SetVendorId("ABIM");
            builder.SetReadAccessLevel(AccessLevel.Public); builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField("SourceElementUniqueId", typeof(string));
            return builder.Finish();
        }

        private sealed class Settings
        {
            public string ParameterName { get; set; }
            public bool UseCurrentView { get; set; }
            public List<string> ViewUniqueIds { get; set; }
            public ElementId TextTypeId { get; set; }
        }
    }
}
