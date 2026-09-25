using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace QA.UserControls.ViewModels
{
    public class SelectSolderRecipeViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;

        public List<SolderRecipeSelectInfo> RecipeInfos { get; set; } = new List<SolderRecipeSelectInfo>();

        public SolderRecipeSelectInfo CurRecipeInfo { get; set; } = null;

        public SelectSolderRecipeViewModel(List<string> recipeNames)
        {
            _eventAggregator = IoC.Get<IEventAggregator>();

            foreach (string name in recipeNames)
            {
                RecipeInfos.Add(new SolderRecipeSelectInfo() { RecipeName = name });
            }
        }

        public void MouseLeaveAction()
        {
            TryClose();
        }

        public void CurRecipeInfoChanged()
        {
            //Console.WriteLine(CurRecipeInfo.RecipeName);
            _eventAggregator.Publish(CurRecipeInfo, action => { Task.Run(action); });
            TryClose();
        }
    }

    public class SolderRecipeSelectInfo
    {
        public string RecipeName { get; set; } = "";
    }
}
