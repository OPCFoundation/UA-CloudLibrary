
namespace MW.Blazor
{
    public class TreeStyle
    {
        public static readonly TreeStyle Bootstrap = new() {
            ExpandNodeIconClass = "fas fa-chevron-right uic-tree__chevron uic-tree--cursor-pointer",
            CollapseNodeIconClass = "fas fa-chevron-down uic-tree__chevron uic-tree--cursor-pointer",
            NodeTitleClass = "p-1",
            NodeTitleSelectableClass = "text-body uic-tree--cursor-pointer",
            NodeTitleSelectedClass = "bg-primary text-white",
            NodeTitleDisabledClass = "text-black-50",
            NodeLoadingClass = "fas fa-spinner"
        };

        public string ExpandNodeIconClass { get; set; }

        public string CollapseNodeIconClass { get; set; }

        public string NodeTitleClass { get; set; }

        public string NodeTitleSelectableClass { get; set; }

        public string NodeTitleSelectedClass { get; set; }

        public string NodeTitleDisabledClass { get; set; }

        public string NodeLoadingClass { get; set; }
    }
}
