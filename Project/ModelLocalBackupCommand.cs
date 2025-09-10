using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AbimToolsMine
{
    /// <summary>
    /// Команда: синхронизировать модель (если она рабочая) и создать:
    /// 1) Актуальную локальную копию в каталоге A
    /// 2) Архивную копию в каталоге A\архив с датой (ГГГГ-ММ-ДД)
    /// Работа пользователя продолжается в исходном открытом документе.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ModelLocalBackupCommand : IExternalCommand
    {
        private static readonly string ArchiveDirectoryName = "Архив"; // Подкаталог для архивов
        private const string FolderParameterName = "ПРО_Путь к папке с заданиями";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "Нет активного документа";
                return Result.Failed;
            }

            Document doc = uidoc.Document;
            try
            {
                if (string.IsNullOrEmpty(doc.PathName) && !doc.IsWorkshared)
                {
                    TaskDialog.Show("Выдать задание", "Документ ещё не сохранён. Сохраните файл перед созданием копии.");
                    return Result.Cancelled;
                }

                // Получаем путь из параметра "ПРО_Путь к папке с заданиями" категории "Сведения о проекте"
                string baseDirectory = GetProjectFolderPath(doc);
                if (string.IsNullOrWhiteSpace(baseDirectory))
                {
                    TaskDialog.Show("Выдать задание", $"Параметр '{FolderParameterName}' не заполнен в 'Сведения о проекте'.");
                    return Result.Cancelled;
                }

                // 1. Сохранение / синхронизация
                EnsureLatestState(doc);

                // 2. Определяем имя файла центральной модели (или самого файла для нерабочих моделей)
                string centralFileName = GetCentralFileName(doc, out string localWorkingFilePath);
                if (string.IsNullOrEmpty(localWorkingFilePath) || !File.Exists(localWorkingFilePath))
                {
                    TaskDialog.Show("Выдать задание", "Локальный файл не найден.");
                    return Result.Failed;
                }

                // 3. Копия в каталоге baseDirectory (имя как у центрального файла)
                string primaryDir = baseDirectory;
                string archiveDir = Path.Combine(baseDirectory, ArchiveDirectoryName);
                Directory.CreateDirectory(primaryDir);
                Directory.CreateDirectory(archiveDir);

                string primaryCopyPath = Path.Combine(primaryDir, centralFileName);

                File.Copy(localWorkingFilePath, primaryCopyPath, true);

                // 4. Архивная копия с датой
                string dateStamp = DateTime.Now.ToString("yyyy-MM-dd"); // ГГГГ-ММ-ДД
                string nameNoExt = Path.GetFileNameWithoutExtension(centralFileName);
                string ext = Path.GetExtension(centralFileName);
                string archiveFileName = $"{nameNoExt}_{dateStamp}{ext}";
                string archiveCopyPath = Path.Combine(archiveDir, archiveFileName);

                // Если за день уже есть копия с таким именем, добавим суффикс времени (часы-минуты) и username, чтобы не перезаписывать
                if (File.Exists(archiveCopyPath))
                {
                    string username = Environment.UserName;
                    archiveFileName = $"{dateStamp}_{DateTime.Now:HH-mm}_{nameNoExt}_{username}{ext}";
                    archiveCopyPath = Path.Combine(archiveDir, archiveFileName);
                }

                File.Copy(localWorkingFilePath, archiveCopyPath, true);

                TaskDialog.Show("Выдать задание", $"Файлы сохранены:\n{primaryCopyPath}\n{archiveCopyPath}");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Ошибка", ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>
        /// Получает значение параметра "ПРО_Путь к папке с заданиями" из Project Information
        /// </summary>
        private string GetProjectFolderPath(Document doc)
        {
            Element projectInfo = doc.ProjectInformation;
            if (projectInfo == null)
                return null;
            Parameter param = projectInfo.LookupParameter(FolderParameterName);
            if (param != null && param.StorageType == StorageType.String)
            {
                return param.AsString();
            }
            return null;
        }

        /// <summary>
        /// Обеспечивает актуальность локального файла: синхронизация для рабочих наборов или простое сохранение.
        /// </summary>
        private void EnsureLatestState(Document doc)
        {
            if (doc.IsWorkshared)
            {
                try
                {
                    TransactWithCentralOptions transact = new TransactWithCentralOptions();
                    SynchronizeWithCentralOptions sync = new SynchronizeWithCentralOptions
                    {
                        Comment = "Auto local backup"
                    };
                    RelinquishOptions relinquish = new RelinquishOptions(false)
                    {
                        CheckedOutElements = false,
                        FamilyWorksets = false,
                        StandardWorksets = false,
                        UserWorksets = false,
                        ViewWorksets = false
                    };
                    sync.SetRelinquishOptions(relinquish);
                    sync.SaveLocalBefore = true;
                    sync.SaveLocalAfter = true;

                    doc.SynchronizeWithCentral(transact, sync);
                }
                catch
                {
                    // Если синхронизация не удалась, просто сохраняем локально
                    doc.Save();
                }
            }
            else
            {
                if (doc.IsModified)
                {
                    doc.Save();
                }
            }
        }

        /// <summary>
        /// Возвращает имя центрального файла (для рабочих моделей) или имя самого файла.
        /// Также возвращает путь к локальному рабочему файлу, который нужно копировать.
        /// </summary>
        private string GetCentralFileName(Document doc, out string localFilePath)
        {
            localFilePath = doc.PathName; // путь к локальному файлу (для обычных и рабочих моделей)

            if (doc.IsWorkshared)
            {
                try
                {
                    ModelPath centralPath = doc.GetWorksharingCentralModelPath();
                    if (centralPath != null)
                    {
                        string centralUserVisible = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralPath);
                        if (!string.IsNullOrEmpty(centralUserVisible))
                        {
                            return Path.GetFileName(centralUserVisible);
                        }
                    }
                }
                catch
                {
                    // Игнорируем и fallback ниже
                }

                // Fallback: пытаемся убрать суффикс _Username из локального имени
                string fallbackName = Path.GetFileName(localFilePath);
                if (!string.IsNullOrEmpty(fallbackName))
                {
                    int idx = fallbackName.LastIndexOf('_');
                    if (idx > 0)
                    {
                        // Возможно, локальный формат: ModelName_Username.rvt
                        string possibleCentral = fallbackName.Substring(0, idx) + Path.GetExtension(fallbackName);
                        return possibleCentral;
                    }
                    return fallbackName;
                }
                return "Model.rvt";
            }
            else
            {
                return Path.GetFileName(localFilePath);
            }
        }
    }
}
