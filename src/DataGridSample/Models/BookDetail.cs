using System.Collections.ObjectModel;

namespace DataGridSample.Models
{
    public class BookDetail
    {
        public string Title { get; set; } = string.Empty;
        public int InStock { get; set; }
        public string Summary { get; set; } = string.Empty;
        public ObservableCollection<AuthorDetail> Authors { get; } = new ObservableCollection<AuthorDetail>();
    }

    public class AuthorDetail
    {
        public string Name { get; set; } = string.Empty;
        public string Contribution { get; set; } = string.Empty;
    }
}
