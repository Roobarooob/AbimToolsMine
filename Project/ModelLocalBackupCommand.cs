using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AbimToolsMine
{
    /// <summary>
    /// �������: ���������������� ������ (���� ��� �������) � �������:
    /// 1) ���������� ��������� ����� � �������� A
    /// 2) �������� ����� � �������� A\����� � ����� (����-��-��)
    /// ������ ������������ ������������ � �������� �������� ���������.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ModelLocalBackupCommand : IExternalCommand
    {
        private static readonly string ArchiveDirectoryName = "�����"; // ���������� ��� �������
        private const string FolderParameterName = "���_���� � ����� � ���������";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "��� ��������� ���������";
                return Result.Failed;
            }

            Document doc = uidoc.Document;
            try
            {
                if (string.IsNullOrEmpty(doc.PathName) && !doc.IsWorkshared)
                {
                    TaskDialog.Show("������ �������", "�������� ��� �� �������. ��������� ���� ����� ��������� �����.");
                    return Result.Cancelled;
                }

                // �������� ���� �� ��������� "���_���� � ����� � ���������" ��������� "�������� � �������"
                string baseDirectory = GetProjectFolderPath(doc);
                if (string.IsNullOrWhiteSpace(baseDirectory))
                {
                    TaskDialog.Show("������ �������", $"�������� '{FolderParameterName}' �� �������� � '�������� � �������'.");
                    return Result.Cancelled;
                }

                // 1. ���������� / �������������
                EnsureLatestState(doc);

                // 2. ���������� ��� ����� ����������� ������ (��� ������ ����� ��� ��������� �������)
                string centralFileName = GetCentralFileName(doc, out string localWorkingFilePath);
                if (string.IsNullOrEmpty(localWorkingFilePath) || !File.Exists(localWorkingFilePath))
                {
                    TaskDialog.Show("������ �������", "��������� ���� �� ������.");
                    return Result.Failed;
                }

                // 3. ����� � �������� baseDirectory (��� ��� � ������������ �����)
                string primaryDir = baseDirectory;
                string archiveDir = Path.Combine(baseDirectory, ArchiveDirectoryName);
                Directory.CreateDirectory(primaryDir);
                Directory.CreateDirectory(archiveDir);

                string primaryCopyPath = Path.Combine(primaryDir, centralFileName);

                File.Copy(localWorkingFilePath, primaryCopyPath, true);

                // 4. �������� ����� � �����
                string dateStamp = DateTime.Now.ToString("yyyy-MM-dd"); // ����-��-��
                string nameNoExt = Path.GetFileNameWithoutExtension(centralFileName);
                string ext = Path.GetExtension(centralFileName);
                string archiveFileName = $"{nameNoExt}_{dateStamp}{ext}";
                string archiveCopyPath = Path.Combine(archiveDir, archiveFileName);

                // ���� �� ���� ��� ���� ����� � ����� ������, ������� ������� ������� (����-������) � username, ����� �� ��������������
                if (File.Exists(archiveCopyPath))
                {
                    string username = Environment.UserName;
                    archiveFileName = $"{dateStamp}_{DateTime.Now:HH-mm}_{nameNoExt}_{username}{ext}";
                    archiveCopyPath = Path.Combine(archiveDir, archiveFileName);
                }

                File.Copy(localWorkingFilePath, archiveCopyPath, true);

                TaskDialog.Show("������ �������", $"����� ���������:\n{primaryCopyPath}\n{archiveCopyPath}");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("������", ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>
        /// �������� �������� ��������� "���_���� � ����� � ���������" �� Project Information
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
        /// ������������ ������������ ���������� �����: ������������� ��� ������� ������� ��� ������� ����������.
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
                    // ���� ������������� �� �������, ������ ��������� ��������
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
        /// ���������� ��� ������������ ����� (��� ������� �������) ��� ��� ������ �����.
        /// ����� ���������� ���� � ���������� �������� �����, ������� ����� ����������.
        /// </summary>
        private string GetCentralFileName(Document doc, out string localFilePath)
        {
            localFilePath = doc.PathName; // ���� � ���������� ����� (��� ������� � ������� �������)

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
                    // ���������� � fallback ����
                }

                // Fallback: �������� ������ ������� _Username �� ���������� �����
                string fallbackName = Path.GetFileName(localFilePath);
                if (!string.IsNullOrEmpty(fallbackName))
                {
                    int idx = fallbackName.LastIndexOf('_');
                    if (idx > 0)
                    {
                        // ��������, ��������� ������: ModelName_Username.rvt
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
