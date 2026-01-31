using System.Collections.ObjectModel;
using DataGridSample.Models;

namespace DataGridSample.ViewModels
{
    public class RowDetailsSelectionViewModel
    {
        public RowDetailsSelectionViewModel()
        {
            Books = new ObservableCollection<BookDetail>
            {
                new BookDetail
                {
                    Title = "Avalonia in Practice",
                    InStock = 12,
                    Summary = "Practical patterns for building desktop and cross-platform apps.",
                    Authors =
                    {
                        new AuthorDetail { Name = "R. Lawson", Contribution = "Lead" },
                        new AuthorDetail { Name = "M. Chen", Contribution = "UI" }
                    }
                },
                new BookDetail
                {
                    Title = "Data Grids Deep Dive",
                    InStock = 5,
                    Summary = "Virtualization, selection, and row details explained with samples.",
                    Authors =
                    {
                        new AuthorDetail { Name = "S. Alvarez", Contribution = "Author" },
                        new AuthorDetail { Name = "K. Novak", Contribution = "Reviewer" }
                    }
                },
                new BookDetail
                {
                    Title = "Reactive MVVM",
                    InStock = 8,
                    Summary = "Techniques for responsive UI and data binding.",
                    Authors =
                    {
                        new AuthorDetail { Name = "J. Patel", Contribution = "Author" }
                    }
                },
                new BookDetail
                {
                    Title = "UI Testing Toolkit",
                    InStock = 3,
                    Summary = "Strategies for reliable UI automation in complex grids.",
                    Authors =
                    {
                        new AuthorDetail { Name = "C. Nguyen", Contribution = "Lead" },
                        new AuthorDetail { Name = "P. Ito", Contribution = "Contributor" }
                    }
                }
            };
        }

        public ObservableCollection<BookDetail> Books { get; }
    }
}
