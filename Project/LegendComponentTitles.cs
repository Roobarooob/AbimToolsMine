using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using View = Autodesk.Revit.DB.View;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class LegendComponentTitles : IExternalCommand
    {
        private static readonly Guid StorageSchemaGuid = new Guid("1C26E79E-407D-4E86-B7C2-774C479D729B");
        private const string ParameterNameField = "ParameterName";
        private const string OffsetMillimetersField = "OffsetMillimeters";
        private const string TextNoteTypeIdField = "TextNoteTypeId";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            if (activeView == null || activeView.ViewType != ViewType.Legend)
            {
                TaskDialog.Show("Заголовки в легендах", "Активный вид должен быть видом легенды.");
                return Result.Cancelled;
            }
            List<Element> legendComponents = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(e => IsLegendComponentOnView(e, activeView))
                .ToList();
           
            if (!legendComponents.Any())
            {
                TaskDialog.Show("Заголовки в легендах", "В текущем выборе не найдено компонентов активной легенды.");
                return Result.Cancelled;
            }

            List<TextNoteType> textNoteTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .OrderBy(t => t.Name)
                .ToList();

            if (!textNoteTypes.Any())
            {
                TaskDialog.Show("Заголовки в легендах", "В проекте не найден тип текста.");
                return Result.Cancelled;
            }

            LegendComponentTitlesSettings settings = LoadSettings(doc);
            ElementId defaultTextNoteTypeId = textNoteTypes.Any(t => t.Id == settings.TextNoteTypeId)
                ? settings.TextNoteTypeId
                : textNoteTypes.First().Id;

            var window = new LegendComponentTitlesWindow(
                textNoteTypes,
                settings.ParameterName,
                settings.OffsetMillimeters,
                defaultTextNoteTypeId);
            RevitWindowOwner.SetOwner(window, commandData.Application);
            if (window.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            string parameterName = window.ParameterName;
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                TaskDialog.Show("Заголовки в легендах", "Не задано имя параметра.");
                return Result.Cancelled;
            }

            if (!TryParseMillimeters(window.OffsetText, out double offsetMillimeters))
            {
                TaskDialog.Show("Заголовки в легендах", "Отступ должен быть числом в миллиметрах.");
                return Result.Cancelled;
            }

            ElementId textNoteTypeId = window.SelectedTextNoteTypeId;
            if (textNoteTypeId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("Заголовки в легендах", "Не выбран тип текстовой метки.");
                return Result.Cancelled;
            }


             double offset = MmToInternal(offsetMillimeters);
            int createdCount = 0;
            int skippedCount = 0;

            using (Transaction transaction = new Transaction(doc, "Заголовки в легендах"))
            {
                transaction.Start();

                foreach (Element legendComponent in legendComponents)
                {
                    ElementType displayedType = GetDisplayedElementType(doc, legendComponent);
                    if (displayedType == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    Parameter parameter = displayedType.LookupParameter(parameterName);
                    string title = ParameterToText(parameter);
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        skippedCount++;
                        continue;
                    }

                    BoundingBoxXYZ bbox = legendComponent.get_BoundingBox(activeView);
                    if (bbox == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    XYZ insertionPoint = GetTopCenterPoint(activeView, bbox, offset);
                    TextNote textNote = TextNote.Create(doc, activeView.Id, insertionPoint, title, textNoteTypeId);
                    doc.Regenerate();
                    CenterTextHorizontally(doc, activeView, textNote, insertionPoint);
                    createdCount++;
                }

                SaveSettings(doc, new LegendComponentTitlesSettings
                {
                    ParameterName = parameterName,
                    OffsetMillimeters = offsetMillimeters,
                    TextNoteTypeId = textNoteTypeId
                });

                transaction.Commit();
            }

            TaskDialog.Show(
                "Заголовки в легендах",
                $"Создано заголовков: {createdCount}\nПропущено компонентов: {skippedCount}");

            return Result.Succeeded;
        }

        private static LegendComponentTitlesSettings LoadSettings(Document doc)
        {
            LegendComponentTitlesSettings settings = new LegendComponentTitlesSettings
            {
                ParameterName = string.Empty,
                OffsetMillimeters = 200,
                TextNoteTypeId = ElementId.InvalidElementId
            };

            Schema schema = Schema.Lookup(StorageSchemaGuid);
            if (schema == null || doc.ProjectInformation == null)
                return settings;

            Entity entity = doc.ProjectInformation.GetEntity(schema);
            if (!entity.IsValid())
                return settings;

            settings.ParameterName = GetStringField(entity, schema, ParameterNameField) ?? string.Empty;
            string offsetText = GetStringField(entity, schema, OffsetMillimetersField);
            if (TryParseMillimeters(offsetText, out double offset))
                settings.OffsetMillimeters = offset;

            ElementId textTypeId = GetElementIdField(entity, schema, TextNoteTypeIdField);
            if (textTypeId != null)
                settings.TextNoteTypeId = textTypeId;

            return settings;
        }

        private static void SaveSettings(Document doc, LegendComponentTitlesSettings settings)
        {
            if (doc.ProjectInformation == null)
                return;

            Schema schema = GetOrCreateSchema();
            Entity entity = new Entity(schema);
            entity.Set(schema.GetField(ParameterNameField), settings.ParameterName ?? string.Empty);
            entity.Set(
                schema.GetField(OffsetMillimetersField),
                settings.OffsetMillimeters.ToString(CultureInfo.InvariantCulture));
            entity.Set(schema.GetField(TextNoteTypeIdField), settings.TextNoteTypeId);
            doc.ProjectInformation.SetEntity(entity);
        }

        private static Schema GetOrCreateSchema()
        {
            Schema schema = Schema.Lookup(StorageSchemaGuid);
            if (schema != null)
                return schema;

            SchemaBuilder builder = new SchemaBuilder(StorageSchemaGuid);
            builder.SetSchemaName("AbimToolsMineLegendComponentTitles");
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetVendorId("ABIM");
            builder.AddSimpleField(ParameterNameField, typeof(string));
            builder.AddSimpleField(OffsetMillimetersField, typeof(string));
            builder.AddSimpleField(TextNoteTypeIdField, typeof(ElementId));
            return builder.Finish();
        }

        private static string GetStringField(Entity entity, Schema schema, string fieldName)
        {
            Field field = schema.GetField(fieldName);
            return field == null ? null : entity.Get<string>(field);
        }

        private static ElementId GetElementIdField(Entity entity, Schema schema, string fieldName)
        {
            Field field = schema.GetField(fieldName);
            return field == null ? null : entity.Get<ElementId>(field);
        }

        private static bool IsLegendComponentOnView(Element element, View view)
        {
            if (element == null || element.Category == null)
                return false;

            return GetElementIdValue(element.Category.Id) == (int)BuiltInCategory.OST_LegendComponents
                && element.OwnerViewId == view.Id;
        }

        private static bool TryParseMillimeters(string text, out double millimeters)
        {
            string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out millimeters);
        }

        private static ElementType GetDisplayedElementType(Document doc, Element legendComponent)
        {
            Parameter displayedTypeParam = legendComponent.get_Parameter(BuiltInParameter.LEGEND_COMPONENT);
            if (displayedTypeParam == null || displayedTypeParam.StorageType != StorageType.ElementId)
                return null;

            ElementId displayedTypeId = displayedTypeParam.AsElementId();
            if (displayedTypeId == ElementId.InvalidElementId)
                return null;

            return doc.GetElement(displayedTypeId) as ElementType;
        }

        private static string ParameterToText(Parameter parameter)
        {
            if (parameter == null)
                return null;

            string value = parameter.AsValueString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return parameter.AsString();
                case StorageType.Integer:
                    return parameter.AsInteger().ToString();
                case StorageType.Double:
                    return parameter.AsDouble().ToString();
                case StorageType.ElementId:
                    ElementId id = parameter.AsElementId();
                    return id == ElementId.InvalidElementId ? null : GetElementIdValue(id).ToString();
                default:
                    return null;
            }
        }

        private static XYZ GetTopCenterPoint(View view, BoundingBoxXYZ bbox, double offset)
        {
            XYZ up = view.UpDirection.Normalize();
            XYZ right = view.RightDirection.Normalize();

            XYZ[] corners =
            {
                new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z),
                new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Min.Z),
                new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z),
                new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Min.Z),
                new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Max.Z),
                new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Max.Z),
                new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Max.Z),
                new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Max.Z)
            };

            double minRight = corners.Min(p => p.DotProduct(right));
            double maxRight = corners.Max(p => p.DotProduct(right));
            double maxUp = corners.Max(p => p.DotProduct(up));

            XYZ origin = view.Origin;
            double originRight = origin.DotProduct(right);
            double originUp = origin.DotProduct(up);

            return origin
                + right.Multiply(((minRight + maxRight) / 2) - originRight)
                + up.Multiply(maxUp - originUp + offset);
        }

        private static void CenterTextHorizontally(Document doc, View view, TextNote textNote, XYZ targetPoint)
        {
            BoundingBoxXYZ textBox = textNote.get_BoundingBox(view);
            if (textBox == null)
                return;

            XYZ right = view.RightDirection.Normalize();
            double textMinRight = textBox.Min.DotProduct(right);
            double textMaxRight = textBox.Max.DotProduct(right);
            double textCenterRight = (textMinRight + textMaxRight) / 2;
            double targetRight = targetPoint.DotProduct(right);
            double delta = targetRight - textCenterRight;

            if (Math.Abs(delta) < 0.000001)
                return;

            ElementTransformUtils.MoveElement(doc, textNote.Id, right.Multiply(delta));
        }

        private static double MmToInternal(double millimeters)
        {
#if R2020
            return UnitUtils.ConvertToInternalUnits(millimeters, DisplayUnitType.DUT_MILLIMETERS);
#else
            return UnitUtils.ConvertToInternalUnits(millimeters, UnitTypeId.Millimeters);
#endif
        }

        private static long GetElementIdValue(ElementId id)
        {
#if R2024 || R2025
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        private class LegendComponentTitlesSettings
        {
            public string ParameterName { get; set; }
            public double OffsetMillimeters { get; set; }
            public ElementId TextNoteTypeId { get; set; }
        }
    }
}
