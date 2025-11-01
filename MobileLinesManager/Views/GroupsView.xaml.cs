using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.ViewModels;

namespace MobileLinesManager.Views
{
    public partial class GroupsView : UserControl
    {
        public GroupsView()
        {
            InitializeComponent();
            
            if (ServiceLocator.ServiceProvider != null)
            {
                DataContext = ServiceLocator.ServiceProvider.GetService<GroupsViewModel>();
            }
        }
    }
}
