using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Diagnostics.Views;
using DiagnosticsControls = Avalonia.Diagnostics.Controls;
using DiagnosticsApplication = Avalonia.Diagnostics.Controls.Application;

namespace Avalonia.Diagnostics.Services
{
    internal sealed class ResourceTreeNodeProvider : IResourceTreeNodeProvider
    {
        private readonly IResourceTreeNodeFactory _factory;

        public ResourceTreeNodeProvider(IResourceTreeNodeFactory factory)
        {
            _factory = factory;
        }

        public ResourceTreeNode[] Create(AvaloniaObject root)
        {
            var roots = new List<ResourceTreeNode>();
            var seenHosts = new HashSet<IResourceHost>();

            void AddHost(IResourceHost host)
            {
                if (seenHosts.Add(host))
                {
                    roots.Add(_factory.CreateHostNode(host, null));
                }
            }

            var application = ResolveApplication(root);
            if (application != null)
            {
                AddHost(application);
            }

            AddTopLevels(root, AddHost);

            if (roots.Count == 0 && root is IResourceHost rootHost)
            {
                AddHost(rootHost);
            }

            return roots.ToArray();
        }

        private static Application? ResolveApplication(AvaloniaObject root)
        {
            if (root is DiagnosticsApplication diagnosticsApp)
            {
                return diagnosticsApp.Instance;
            }

            return Application.Current;
        }

        private static void AddTopLevels(AvaloniaObject root, System.Action<IResourceHost> addHost)
        {
            switch (root)
            {
                case DiagnosticsApplication diagnosticsApp:
                    AddTopLevels(diagnosticsApp.Items, addHost);
                    break;
                case DiagnosticsControls.TopLevelGroup group:
                    AddTopLevels(group.Items, addHost);
                    break;
                case TopLevel topLevel:
                    if (topLevel is not MainWindow && topLevel is IResourceHost host)
                    {
                        addHost(host);
                    }
                    break;
            }
        }

        private static void AddTopLevels(IReadOnlyList<TopLevel> items, System.Action<IResourceHost> addHost)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var topLevel = items[i];

                if (topLevel is MainWindow)
                {
                    continue;
                }

                if (topLevel is IResourceHost host)
                {
                    addHost(host);
                }
            }
        }
    }
}
