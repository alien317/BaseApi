using Api.Common.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.ComponentModel;

namespace Api.Common.Utils
{
    public class ControllerDocumentationConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel model) 
        {
            if(model == null) { return; }

            foreach(var attribute in model.Attributes)
            {
                if(attribute.GetType() == typeof(ControllerNameAttribute))
                {
                    var routeAttribute = (ControllerNameAttribute)attribute;
                    if (!string.IsNullOrEmpty(routeAttribute.Name))
                    {
                        model.ControllerName = routeAttribute.Name;
                    }
                }
            }
        }
    }
}
