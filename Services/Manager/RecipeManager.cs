using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using QA.Business.Recipe;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    public class RecipeManager
    {
        //public string ReciprDirectory = ConfigurationManager.AppSettings["recipePath"].Replace("{BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
        public string ReciprDirectory = @"D:\QKProject\Setting\Config\Recipe";

        public string FileSuffix { private set; get; } = @".rcp";

        public ObservableCollection<LaserSprayRecipe> Recipes { get; set; } = new ObservableCollection<LaserSprayRecipe>();

        public RecipeManager()
        {
            if (!Directory.Exists(ReciprDirectory))
            {
                Directory.CreateDirectory(ReciprDirectory);
            }
            LoadAllRecipes();
        }

        public bool LoadAllRecipes()
        {
            try
            {
                Recipes.Clear();
                var fileNames = Directory.GetFiles(ReciprDirectory).Where(t => new FileInfo(t).Extension.Equals(FileSuffix));
                foreach (var fileName in fileNames)
                {
                    var recipe = new LaserSprayRecipe();
                    if (LoadRecipe<LaserSprayRecipe>(fileName, ref recipe))
                    {
                        Recipes.Add(recipe);
                    }
                }
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载全部配方异常:{ex.Message},{ex.StackTrace}");
            }
            return false;
        }

        public bool SaveAllRecipes()
        {
            try
            {
                var fileNames = Directory.GetFiles(ReciprDirectory).Where(t => new FileInfo(t).Extension.Equals(FileSuffix));
                foreach (string fileName in fileNames)
                {
                    File.Delete(fileName);
                }
                foreach (LaserSprayRecipe recipe in Recipes)
                {
                    SaveRecipe<LaserSprayRecipe>(recipe.RecipeName, recipe);
                    //SerializerByJson.SaveToJson($"{ReciprDirectory}\\{recipe.RecipeName}{FileSuffix}", recipe);
                }
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存全部配方异常:{ex.Message},{ex.StackTrace}");
            }
            return false;
        }

        public bool LoadRecipe<T>(string filePath, ref T recipe)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"配方文件{filePath}不存在");
                    return false;
                }
                var getObj = (T)SerializerByJson.LoadFromJson(filePath, recipe.GetType());
                if (getObj != null)
                {
                    recipe = getObj.DeepCopy();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载{Path.GetFileName(filePath)}配方异常:{ex.Message},{ex.StackTrace}");
            }
            return false;
        }

        public bool SaveRecipe<T>(string recipeName, T recipe)
        {
            try
            {
                if (string.IsNullOrEmpty(recipeName))
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"配方名称为空，保存失败");
                    return false;
                }
                string filePath = Path.Combine(ReciprDirectory, $"{recipeName}{FileSuffix}");
                SerializerByJson.SaveToJson(filePath, recipe);
                return true;
            }
            catch (System.Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存{recipeName}配方异常:{ex.Message},{ex.StackTrace}");
            }
            return false;
        }

        public LaserSprayRecipe GetRecipe(string recipeName)
        {
            List<LaserSprayRecipe> recipes = Recipes.ToList();

            return recipes.FindLast(t =>
            {
                if (string.IsNullOrEmpty(t.RecipeName))
                {
                    return false;
                }
                return t.RecipeName.Equals(recipeName, StringComparison.OrdinalIgnoreCase);
            });
        }

        public List<string> GetAllRecipeNames()
        {
            List<string> names = new List<string>();
            foreach (LaserSprayRecipe recipe in Recipes)
            {
                names.Add(recipe?.RecipeName);
            }
            return names;
        }

    }
}
