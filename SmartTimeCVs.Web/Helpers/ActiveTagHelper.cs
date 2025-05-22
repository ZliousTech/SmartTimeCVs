using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SmartTimeCVs.Web.Helpers
{
    [HtmlTargetElement("li", Attributes = "active-when")]
    public class ActiveTagHelper : TagHelper
    {
        public string? ActiveWhen { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContextData { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(ActiveWhen))
                return;

            var currentController = ViewContextData?.RouteData.Values["controller"]?.ToString();

            // Split the ActiveWhen values by comma and check if any matches the current controller
            var controllers = ActiveWhen.Split(',').Select(c => c.Trim());

            if (controllers.Contains(currentController))
            {
                if (output.Attributes.ContainsName("class"))
                    output.Attributes.SetAttribute("class", $"{output.Attributes["class"].Value} active open");
                else
                    output.Attributes.SetAttribute("class", "active open");
            }
        }
    }
}
