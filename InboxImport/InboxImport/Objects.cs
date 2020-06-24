using System.Collections.Generic;

namespace InboxImport
{
    public class InboxList
    {
        public int InboxOptions { get; set; }
        public List<Inbox> Inboxes { get; set; }
    }

    public class Inbox
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class ArchiveList
    {
        public List<Archive> Archives { get; set; }
    }

    public class Archive
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Parent { get; set; }
        public int Properties { get; set; }
    }

    internal class FilesList
    {
        public List<UploadedFile> Files { get; set; }
    }

    internal class UploadedFile
    {
        public string Name { get; set; }
    }

    internal class Indexer
    {
        public List<Field> Fields { get; set; }
        public List<File> Files { get; set; }
        public Indexer()
        {
            Fields = new List<Field>();
            Files = new List<File>();
        }
    }

    internal class Field
    {
        public string Name;
        public string Value;
        public Field() { }
        public Field(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }

    internal class File
    {
        public string Name;
        public File() { }
        public File(string Name)
        {
            this.Name = Name;
        }
    }
}
