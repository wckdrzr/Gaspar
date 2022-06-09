using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WCKDRZR.CSharpExporter.Extensions;

namespace WCKDRZR.CSharpExporter.Models
{
    public class CSharpFiles : IEnumerable
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

        public int Count => Files.Count();

        public void DeDuplicateControllerNames(ControllerTypeConfiguration config)
        {
            List<string> UsedControllerNames = new();
            foreach (CSharpFile file in Files)
            {
                foreach (Controller controller in file.Controllers)
                {
                    int index = -1;
                    string newName = controller.OutputClassName;
                    while (UsedControllerNames.Contains(newName))
                    {
                        newName = controller.OutputClassName +
                            (index >= 0 ? config.ServiceName.ToProper() : "") +
                            (index > 0 ? index : "");
                        index++;
                    }
                    controller.OutputClassName = newName;
                    UsedControllerNames.Add(newName);
                }
            }
        }

        public List<string> CustomTypes()
        {
            List<string> types = new();

            foreach (CSharpFile file in Files)
            {
                foreach (Controller controller in file.Controllers)
                {
                    foreach (ControllerAction action in controller.Actions)
                    {
                        foreach (Parameter parameter in action.Parameters)
                        {
                            AddUniqueCustomType(ref types, parameter.Type);
                        }
                        AddUniqueCustomType(ref types, action.ReturnType);
                        AddUniqueCustomType(ref types, action.ReturnTypeOverride);
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

    public class CSharpFile
    {
        public string Path { get; set; }
        public List<Model> Models { get; set; }
        public List<EnumModel> Enums { get; set; }
        public List<Controller> Controllers { get; set; }

        public bool HasModels => Models.Count > 0 || Enums.Count > 0;
        public bool HasControllers => Controllers.Count > 0;

        public void Concat(CSharpFile file)
        {
            Models.Concat(file.Models);
            Enums.Concat(file.Enums);
            Controllers.Concat(file.Controllers);
        }
    }
}