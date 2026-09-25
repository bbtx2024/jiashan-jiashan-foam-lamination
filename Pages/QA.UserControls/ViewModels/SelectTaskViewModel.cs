using System.Collections.ObjectModel;
using Caliburn.Micro;
using QA.Business.Manager;

namespace QA.UserControls.ViewModels
{
    public class SelectTaskViewModel : Screen
    {
        private readonly TaskManager _taskManager = null;

        public override string DisplayName { get; set; } = "制程选择";

        public ObservableCollection<string> TaskNames { get; set; } = new ObservableCollection<string>();

        public string SelectedTaskName { get; set; } = "";

        public SelectTaskViewModel()
        {
            _taskManager = IoC.Get<TaskManager>();
            Init();
        }

        public void Init()
        {
            TaskNames.Clear();
            foreach (var task in _taskManager.Tasks)
            {
                TaskNames.Add(task.ProcedureName);
            }
        }

        public void Confirm()
        {
            TryClose(true);
        }

        public void Cancel()
        {
            TryClose(false);
        }
    }
}
