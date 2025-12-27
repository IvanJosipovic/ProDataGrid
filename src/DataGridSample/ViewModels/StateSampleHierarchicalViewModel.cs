using System.Collections.ObjectModel;
using Avalonia.Controls.DataGridHierarchical;

namespace DataGridSample.ViewModels
{
    public class StateSampleHierarchicalViewModel
    {
        public class Node
        {
            public Node(int id, string name)
            {
                Id = id;
                Name = name;
                Children = new ObservableCollection<Node>();
            }

            public int Id { get; }

            public string Name { get; }

            public ObservableCollection<Node> Children { get; }
        }

        public StateSampleHierarchicalViewModel()
        {
            var root = BuildSample();

            var options = new HierarchicalOptions<Node>
            {
                ChildrenSelector = node => node.Children,
            };

            Model = new HierarchicalModel<Node>(options);
            Model.SetRoot(root);
        }

        public HierarchicalModel<Node> Model { get; }

        private static Node BuildSample()
        {
            var root = new Node(0, "Root");

            var alpha = new Node(1, "Alpha");
            alpha.Children.Add(new Node(2, "Alpha-1"));
            alpha.Children.Add(new Node(3, "Alpha-2"));

            var beta = new Node(4, "Beta");
            var betaChild = new Node(5, "Beta-1");
            betaChild.Children.Add(new Node(6, "Beta-1-a"));
            beta.Children.Add(betaChild);

            root.Children.Add(alpha);
            root.Children.Add(beta);

            return root;
        }
    }
}
