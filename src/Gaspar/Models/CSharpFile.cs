using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.Gaspar.Extensions;

namespace WCKDRZR.Gaspar.Models
{
    internal class CSharpFiles : IEnumerable
    {
        List<CSharpFile> Files { get; set; }

        public CSharpFiles()
        {
            Files = new();
        }

        public void Add(CSharpFile file)
        {
            if (file.HasModels || file.HasControllers)
            {
                CSharpFile existingFile = Files.SingleOrDefault(f => f.Path == file.Path);
                if (existingFile == null)
                {
                    Files.Add(file);
                }
                else
                {
                    existingFile.Concat(file);
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            return Files.GetEnumerator();
        }

        public List<CSharpFile> ToList()
        {
            return Files;
        }

        public int Count => Files.Count();

        public List<CSharpFile> FilesWithControllersWithActionsForType(OutputType type) => Files.Where(f => f.HasControllers && f.ControllersWithActionsForType(type).Count > 0).ToList();

        public void DeDuplicateControllerAndActionNames(OutputType forOutputType, bool allTypes, string serviceName)
        {
            List<string> UsedControllerNames = new();
            foreach (CSharpFile file in allTypes ? Files : FilesWithControllersWithActionsForType(forOutputType))
            {
                foreach (Controller controller in allTypes ? file.Controllers : file.ControllersWithActionsForType(forOutputType))
                {
                    int controllerIndex = -1;
                    string newControllerName = controller.ControllerName;
                    while (UsedControllerNames.Contains(newControllerName))
                    {
                        newControllerName = controller.ControllerName +
                            (controllerIndex >= 0 ? serviceName.ToProper() : "") +
                            (controllerIndex > 0 ? controllerIndex : "");
                        controllerIndex++;
                    }
                    controller.OutputClassName = newControllerName;
                    UsedControllerNames.Add(newControllerName);


                    List<string> UsedActionNames = new();
                    foreach (ControllerAction action in allTypes ? controller.Actions : controller.ActionsForType(forOutputType))
                    {
                        int actionIndex = -1;
                        string newActionName = action.ActionName;
                        while (UsedActionNames.Contains(newActionName))
                        {
                            newActionName = action.ActionName +
                                (actionIndex >= 0 ? action.Parameters.FunctionNameExtension() : "") +
                                (actionIndex > 0 ? actionIndex : "");
                            actionIndex++;
                        }
                        action.OutputActionName = newActionName;
                        UsedActionNames.Add(newActionName);
                    }
                }
            }
        }

        public List<string> CustomTypes(OutputType forOutputType, bool allTypes)
        {
            List<string> types = new();

            foreach (CSharpFile file in Files)
            {
                foreach (Controller controller in file.Controllers)
                {
                    foreach (ControllerAction action in allTypes ? controller.Actions : controller.ActionsForType(forOutputType))
                    {
                        foreach (Parameter parameter in action.Parameters)
                        {
                            AddUniqueCustomType(ref types, parameter.Type);
                        }
                        AddUniqueCustomType(ref types, action.ReturnTypeOverride);
                        if (action.ReturnTypeOverride == null) { AddUniqueCustomType(ref types, action.ReturnType); }
                        AddUniqueCustomType(ref types, action.BodyType);
                    }
                }
            }

            return types;
        }

        private void AddUniqueCustomType(ref List<string> types, TypeSyntax newType)
        {
            if (newType is GenericNameSyntax)
            {
                newType = ((GenericNameSyntax)newType).TypeArgumentList.Arguments[0];
            }
            if (newType != null && newType is not PredefinedTypeSyntax && newType is not NullableTypeSyntax)
            {
                AddUniqueCustomType(ref types, newType.ToString());
            }
        }
        private void AddUniqueCustomType(ref List<string> types, string newType)
        {
            if (newType != null)
            {
                if (newType.EndsWith("[]")) { newType = newType[..^2]; }
                if (types.SingleOrDefault(t => t == newType) == null)
                {
                    types.Add(newType);
                }
            }
        }
    }

    internal class CSharpFile
    {
        public string Path { get; set; }
        public List<Model> Models { get; set; }
        public List<EnumModel> Enums { get; set; }
        public List<Controller> Controllers { get; set; }

        public bool HasModels => Models.Count > 0 || Enums.Count > 0;
        public bool HasControllers => Controllers.Count > 0;

        public List<EnumModel> EnumsForType(OutputType type) => Enums.Where(a => a.ExportFor.HasFlag(type)).ToList();
        public List<Controller> ControllersWithActionsForType(OutputType type) => Controllers.Where(c => c.ActionsForType(type).Count() > 0).ToList();

        public List<Model> ModelsForType(OutputType type)
        {
            List<Model> modelsOfType = Models.Where(a => a.ExportFor.HasFlag(type)).ToList();

            List<Model> modelsWithType = Models.Except(modelsOfType).Where(a => a.Properties.Any(p => p.ExportFor.HasFlag(type)) || a.Fields.Any(p => p.ExportFor.HasFlag(type))).ToList();
            for (int i = 0; i < modelsWithType.Count; i++)
            {
                modelsWithType[i].Properties = modelsWithType[i].Properties.Where(a => a.ExportFor.HasFlag(type)).ToList();
                modelsWithType[i].Fields = modelsWithType[i].Fields.Where(a => a.ExportFor.HasFlag(type)).ToList();
            }

            return modelsOfType.Concat(modelsWithType).ToList();
        }

        public void Concat(CSharpFile file)
        {
            Models.Concat(file.Models);
            Enums.Concat(file.Enums);
            Controllers.Concat(file.Controllers);
        }
    }
}