using SQLite;

namespace stepstones.Models
{
    [Table("MediaItems")]
    public class MediaItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FileName { get; set; }

        [Unique]
        public string FilePath { get; set; }

        public MediaType FileType { get; set; }

        public string? Tags { get; set; }

        public TimeSpan Duration { get; set; }

        public string? ThumbnailPath { get; set; }
    }
}
