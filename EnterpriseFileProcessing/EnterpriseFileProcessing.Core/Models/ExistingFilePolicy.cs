namespace EnterpriseFileProcessing.Core.Models;

public enum ExistingFilePolicy
{
    SkipExisting = 1,
    OverwriteExisting = 2,
    OverwriteOnlyIfSourceIsNewer = 3,
    RenameAutomatically = 4,
    CreateVersion = 5,
    FailJob = 6,
    MergeFolderStructure = 7
}
